# NXP RCS · Brand Spec

从用户两版登录稿（`0.png` 深色 / `1.png` 浅色）实测提取。内网工厂调度控制台，不是 SaaS。

一句话：深炭或冷薄荷底 + 洁净室场景，青绿主色只做识别与主按钮，双品牌锁在表单卡顶。

## Tokens（OKLch）

```css
:root {
  /* Light — 对应 1.png */
  --bg: oklch(97.2% 0.008 220);
  --surface: oklch(100% 0 0);
  --fg: oklch(25% 0 0);
  --muted: oklch(55% 0.01 220);
  --border: oklch(90% 0.01 220);
  --accent: oklch(70% 0.12 180);

  /* 实测绑定（实现时可用 hex 等价） */
  /* --bg: #f0f7f9; --surface: #fff; --fg: #1f1f1f; --muted: #7a8a90; --border: #d7e0e4; --accent: #15b7a8; */
}

html.dark {
  --bg: oklch(22% 0.005 240);
  --surface: oklch(24% 0.005 240);
  --fg: oklch(93% 0 0);
  --muted: oklch(68% 0.01 220);
  --border: oklch(35% 0.01 240);
  --accent: oklch(70% 0.12 180);
  /* --bg: #1b1b1b; --surface: #1c1c1c; --fg: #f0f0f0; --muted: #9aa3a8; --border: #333; --accent: #15b7a8; */
}
```

## 实测色值

| 角色 | 浅色 | 深色 |
| --- | --- | --- |
| 页面/叙事底 | `#f0f7f9` | `#1b1b1b` |
| 表单卡 | `#ffffff` | `#1c1c1c` |
| 正文 | `#1f1f1f` | `#f0f0f0` |
| 次要说明 | `#7a8a90` | `#9aa3a8` |
| 主色（青绿） | `#15b7a8` | 同左 |
| 主按钮渐变终点 | `#10a6e6` | 同左 |
| 品牌锁 SIASUN | `#043885` | 浅色稿同；深色稿可用主色描边 |
| 品牌锁 DUCO | `#025aff` | 同左 |

## 字体

- Display / 标题：系统无衬线加粗（Segoe UI / PingFang SC / Microsoft YaHei），工业控制台气质，不用营销衬线。
- Body：同一无衬线栈，常规字重。
- Mono：设备缩写标签、产品线名（`NXP RCS · AMHS CONTROL`）。

## 视觉规则（3–5）

1. 登录是左右分栏：左叙事 + 厂内场景，右表单卡；禁止居中 SaaS 色斑卡。
2. 全页最多一种主色；主按钮可用青绿→天蓝水平渐变，算作同一主色动作。
3. 背景允许极淡技术网格；禁止紫雾、霓虹、营销插画。
4. 设备缩写标签（AMHS / OHT / AMR / STK / E-Pass / ERACK）用细描边胶囊，主色字，不抢主按钮。
5. 文案语气是授权控制台，不是注册转化；强调厂内调度与授权账号。
