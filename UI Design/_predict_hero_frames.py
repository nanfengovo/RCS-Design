"""Predict dense, low-distortion motion frames from cropped cleanroom stills.

Prior ±2 pipeline used strong perspective warps (looked twisted).
This version: tiny pan + path-local nudge + cyan pulse, many steps, WebP loop.
"""
from __future__ import annotations

from pathlib import Path
import math
import numpy as np
from PIL import Image, ImageEnhance, ImageFilter, ImageChops

root = Path(__file__).resolve().parent
assets = root / "assets"

# Half-span of unique keyframes on each side of center (total unique = 2*SPAN+1)
SPAN = 6
# Extra interpolated steps between each keyframe for the WebP loop
SUBSTEPS = 2


def boost_cyan_glow(im: Image.Image, amount: float) -> Image.Image:
    """Pulse glowing cyan path pixels. amount ~ 0..1."""
    arr = np.asarray(im).astype(np.float32)
    r, g, b = arr[..., 0], arr[..., 1], arr[..., 2]
    cyan = (b > 90) & (g > 80) & (b > r + 25) & (g > r + 10) & ((r + g + b) < 620)
    if not cyan.any():
        return im
    factor = 1.0 + 0.28 * amount
    out = arr.copy()
    out[..., 1] = np.where(cyan, np.clip(g * factor, 0, 255), g)
    out[..., 2] = np.where(cyan, np.clip(b * factor * 1.03, 0, 255), b)
    glow = np.zeros_like(arr)
    glow[cyan] = out[cyan]
    glow_im = Image.fromarray(glow.astype(np.uint8)).filter(
        ImageFilter.GaussianBlur(radius=2 + 2.5 * amount)
    )
    base = Image.fromarray(out.astype(np.uint8))
    return ImageChops.screen(base, ImageEnhance.Brightness(glow_im).enhance(0.22 * amount))


def soft_pan(im: Image.Image, t: float) -> Image.Image:
    """Global micro pan/dolly. t in [-1, 1]. No perspective — avoids twist."""
    w, h = im.size
    # ~0.6% of width / 0.4% of height at full excursion
    dx = w * 0.006 * t
    dy = h * 0.004 * math.sin(t * math.pi * 0.5)
    s = 1.0 + 0.008 * t
    # Affine: scale about center then translate
    cx, cy = w / 2, h / 2
    a = s
    e = s
    c = cx - s * cx + dx
    f = cy - s * cy + dy
    return im.transform(
        (w, h),
        Image.AFFINE,
        (a, 0, c, 0, e, f),
        Image.Resampling.BICUBIC,
        fillcolor=im.getpixel((2, 2)),
    )


def path_nudge(im: Image.Image, t: float) -> Image.Image:
    """Very light local shift near cyan path / AMR zone. t in [-1, 1]."""
    w, h = im.size
    arr = np.asarray(im).astype(np.float32)
    yy, xx = np.mgrid[0:h, 0:w]
    cx, cy = w * 0.48, h * 0.62
    rx, ry = w * 0.28, h * 0.22
    # Cap displacement to ~0.8% of width — was 5.5% before (main smear source)
    dx = rx * 0.018 * t
    dy = ry * 0.012 * math.sin(t * 1.1)
    ell = ((xx - cx) / rx) ** 2 + ((yy - cy) / ry) ** 2
    band = np.exp(-((ell - 1.0) ** 2) / 0.22)
    zone = np.exp(
        -(((xx - cx) / (w * 0.22)) ** 2 + ((yy - h * 0.68) / (h * 0.18)) ** 2)
    )
    weight = np.clip(band * 0.35 + zone * 0.55, 0, 1)

    map_x = np.clip(xx - dx * weight, 0, w - 1)
    map_y = np.clip(yy - dy * weight, 0, h - 1)
    x0 = np.floor(map_x).astype(np.int32)
    y0 = np.floor(map_y).astype(np.int32)
    x1 = np.clip(x0 + 1, 0, w - 1)
    y1 = np.clip(y0 + 1, 0, h - 1)
    wx = (map_x - x0)[..., None]
    wy = (map_y - y0)[..., None]
    Ia = arr[y0, x0]
    Ib = arr[y0, x1]
    Ic = arr[y1, x0]
    Id = arr[y1, x1]
    out = (
        Ia * (1 - wx) * (1 - wy)
        + Ib * wx * (1 - wy)
        + Ic * (1 - wx) * wy
        + Id * wx * wy
    )
    return Image.fromarray(np.clip(out, 0, 255).astype(np.uint8))


def predict_frame(base: Image.Image, u: float) -> Image.Image:
    """u in [-1, 1] continuous."""
    frame = soft_pan(base, u)
    frame = path_nudge(frame, u)
    glow = 0.30 + 0.40 * (0.5 + 0.5 * math.sin((u + 1) * math.pi))
    frame = boost_cyan_glow(frame, glow)
    return frame


def fit_canvas(base: Image.Image, size=(1280, 720)) -> Image.Image:
    tw, th = size
    scale = min(tw / base.width, th / base.height)
    nw, nh = int(base.width * scale), int(base.height * scale)
    resized = base.resize((nw, nh), Image.Resampling.LANCZOS)
    canvas = Image.new("RGB", size, resized.getpixel((2, 2)))
    canvas.paste(resized, ((tw - nw) // 2, (th - nh) // 2))
    return canvas


def key_name(i: int) -> str:
    """Map integer index -SPAN..SPAN to filename suffix: m6..m1, 0, p1..p6."""
    if i == 0:
        return "0"
    if i < 0:
        return f"m{abs(i)}"
    return f"p{i}"


def build_loop(base_path: Path, out_stem: str, size=(1280, 720)):
    base = Image.open(base_path).convert("RGB")
    canvas = fit_canvas(base, size)

    # Unique keyframes at integer steps
    keyframes: dict[int, Image.Image] = {}
    for i in range(-SPAN, SPAN + 1):
        u = i / float(SPAN)
        if i == 0:
            fr = boost_cyan_glow(canvas.copy(), 0.38)
        else:
            fr = predict_frame(canvas, u)
        keyframes[i] = fr
        path = assets / f"{out_stem}-f{key_name(i)}.png"
        fr.save(path, optimize=True)
        print("wrote", path.name, fr.size)

    # Dense ping-pong for WebP: interpolate between keyframes
    loop_frames: list[Image.Image] = []
    # Forward -SPAN -> +SPAN, then back +SPAN-1 -> -SPAN+1
    forward = list(range(-SPAN, SPAN + 1))
    backward = list(range(SPAN - 1, -SPAN, -1))
    for a, b in zip(forward, forward[1:] + [None]):
        if b is None:
            break
        for s in range(SUBSTEPS + 1):
            # skip last substep of each pair except we add full frame at start of next
            if s == SUBSTEPS:
                continue
            t = s / (SUBSTEPS + 1)
            if t == 0:
                loop_frames.append(keyframes[a])
            else:
                blend = Image.blend(keyframes[a], keyframes[b], t)
                loop_frames.append(blend)
    loop_frames.append(keyframes[SPAN])
    for a, b in zip(backward, backward[1:] + [None]):
        if b is None:
            break
        for s in range(SUBSTEPS + 1):
            if s == SUBSTEPS:
                continue
            t = s / (SUBSTEPS + 1)
            if t == 0:
                loop_frames.append(keyframes[a])
            else:
                loop_frames.append(Image.blend(keyframes[a], keyframes[b], t))

    # ~36–40 ms per frame ≈ 25–28 fps feel; many frames keep it silky
    duration_ms = 40
    webp = assets / f"{out_stem}-loop.webp"
    loop_frames[0].save(
        webp,
        save_all=True,
        append_images=loop_frames[1:],
        duration=duration_ms,
        loop=0,
        method=4,
        quality=84,
    )
    print("wrote", webp.name, "frames", len(loop_frames), "duration_ms", duration_ms)
    return webp


def main():
    dark = assets / "hero-base-dark.png"
    light = assets / "hero-base-light.png"
    if not light.exists():
        light = assets / "login-hero-cleanroom.png"
    build_loop(dark, "hero-dark")
    build_loop(light, "hero-light")


if __name__ == "__main__":
    main()
