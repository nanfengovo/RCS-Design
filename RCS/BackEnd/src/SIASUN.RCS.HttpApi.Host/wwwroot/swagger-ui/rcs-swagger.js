/**
 * RCS Swagger toolbar: culture <select> (scales to N languages) + theme toggle.
 * Positioned under topbar to avoid overlapping "Select a definition".
 * Cultures from /swagger/rcs-meta.json.
 */
(function () {
  const CULTURE_KEY = "rcs-swagger-culture";
  const THEME_KEY = "rcs-swagger-theme";

  const sunSvg =
    '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 18a6 6 0 1 0 0-12 6 6 0 0 0 0 12zm0 4a1 1 0 0 1-1-1v-1a1 1 0 1 1 2 0v1a1 1 0 0 1-1 1zm0-18a1 1 0 0 1-1-1V2a1 1 0 1 1 2 0v1a1 1 0 0 1-1 1zm10 9a1 1 0 0 1-1 1h-1a1 1 0 1 1 0-2h1a1 1 0 0 1 1 1zM4 12a1 1 0 0 1-1 1H2a1 1 0 1 1 0-2h1a1 1 0 0 1 1 1zm14.95 6.95a1 1 0 0 1-1.41 0l-.71-.7a1 1 0 1 1 1.41-1.42l.71.71a1 1 0 0 1 0 1.41zM6.76 6.76a1 1 0 0 1-1.41 0l-.71-.71A1 1 0 0 1 6.05 3.63l.71.71a1 1 0 0 1 0 1.42zm0 10.49a1 1 0 0 1 0 1.41l-.71.71A1 1 0 1 1 3.63 17.95l.71-.71a1 1 0 0 1 1.42 0zm10.49-10.49a1 1 0 0 1 0-1.42l.71-.71a1 1 0 1 1 1.41 1.41l-.71.72a1 1 0 0 1-1.41 0z"/></svg>';
  const moonSvg =
    '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M21 14.5A8.5 8.5 0 1 1 9.5 3a7 7 0 1 0 11.5 11.5z"/></svg>';

  function applyTheme(theme) {
    const t = theme === "dark" ? "dark" : "light";
    document.documentElement.setAttribute("data-rcs-theme", t);
    localStorage.setItem(THEME_KEY, t);
    const btn = document.querySelector("[data-rcs-theme-toggle]");
    if (btn) {
      btn.innerHTML = t === "dark" ? sunSvg : moonSvg;
      btn.title = t === "dark" ? "Switch to light" : "Switch to dark";
      btn.setAttribute("aria-label", btn.title);
    }
  }

  function currentCulture(meta) {
    const saved = localStorage.getItem(CULTURE_KEY);
    if (saved && meta.cultures.some((c) => c.id === saved)) return saved;
    return meta.defaultCulture || "zh-Hans";
  }

  function buildUrls(meta, culture) {
    return (meta.groups || []).map((g) => ({
      url: `/swagger/${g.id}.${culture}/swagger.json`,
      name: g.name
    }));
  }

  function getOauthConfig() {
    try {
      if (window.ui && typeof window.ui.getConfigs === "function") {
        const cfg = window.ui.getConfigs();
        return {
          clientId: cfg.clientId || "RCS_Swagger",
          scopes: Array.isArray(cfg.scopes) ? cfg.scopes : ["RCS"],
          usePkceWithAuthorizationCodeGrant: true
        };
      }
    } catch (_) {}
    return {
      clientId: "RCS_Swagger",
      scopes: ["RCS"],
      usePkceWithAuthorizationCodeGrant: true
    };
  }

  function remountSwagger(urls) {
    const host = document.getElementById("swagger-ui");
    if (!host || typeof SwaggerUIBundle !== "function") {
      location.reload();
      return;
    }

    const oauth = getOauthConfig();
    host.innerHTML = "";

    const config = {
      urls: urls,
      "urls.primaryName": urls[0] && urls[0].name,
      dom_id: "#swagger-ui",
      deepLinking: true,
      presets: [SwaggerUIBundle.presets.apis, SwaggerUIStandalonePreset],
      plugins: [SwaggerUIBundle.plugins.DownloadUrl],
      layout: "StandaloneLayout",
      validatorUrl: null,
      oauth2RedirectUrl: new URL("oauth2-redirect.html", window.location.href).href
    };

    window.ui = SwaggerUIBundle(config);
    if (window.ui && typeof window.ui.initOAuth === "function") {
      window.ui.initOAuth({
        clientId: oauth.clientId || "RCS_Swagger",
        scopes: oauth.scopes || ["RCS"],
        usePkceWithAuthorizationCodeGrant: true
      });
    }
  }

  function setCulture(meta, culture, remount) {
    localStorage.setItem(CULTURE_KEY, culture);
    const sel = document.querySelector("[data-rcs-culture-select]");
    if (sel && sel.value !== culture) sel.value = culture;
    if (remount) remountSwagger(buildUrls(meta, culture));
  }

  function syncTopbarOffset() {
    const topbar = document.querySelector(".swagger-ui .topbar");
    const h = topbar ? Math.ceil(topbar.getBoundingClientRect().height) : 56;
    document.documentElement.style.setProperty("--rcs-topbar-offset", h + "px");
  }

  function mountToolbar(meta) {
    if (document.querySelector(".rcs-swagger-toolbar")) return;

    const bar = document.createElement("div");
    bar.className = "rcs-swagger-toolbar";
    bar.setAttribute("role", "toolbar");
    bar.setAttribute("aria-label", "Swagger language and theme");

    const langWrap = document.createElement("div");
    langWrap.className = "rcs-lang-wrap";

    const langLabel = document.createElement("span");
    langLabel.className = "rcs-lang-label";
    langLabel.textContent = "Lang";
    langWrap.appendChild(langLabel);

    // One select scales to any number of cultures from rcs-meta.json
    const select = document.createElement("select");
    select.setAttribute("data-rcs-culture-select", "1");
    select.title = "Documentation language";
    select.setAttribute("aria-label", "Documentation language");
    (meta.cultures || []).forEach((c) => {
      const opt = document.createElement("option");
      opt.value = c.id;
      opt.textContent = c.label || c.id;
      select.appendChild(opt);
    });
    select.addEventListener("change", () => setCulture(meta, select.value, true));
    langWrap.appendChild(select);
    bar.appendChild(langWrap);

    const sep = document.createElement("span");
    sep.className = "rcs-sep";
    sep.setAttribute("aria-hidden", "true");
    bar.appendChild(sep);

    const themeBtn = document.createElement("button");
    themeBtn.type = "button";
    themeBtn.className = "rcs-icon-btn";
    themeBtn.setAttribute("data-rcs-theme-toggle", "1");
    themeBtn.addEventListener("click", () => {
      const cur = document.documentElement.getAttribute("data-rcs-theme") || "light";
      applyTheme(cur === "dark" ? "light" : "dark");
    });
    bar.appendChild(themeBtn);

    document.body.appendChild(bar);
    syncTopbarOffset();
    window.addEventListener("resize", syncTopbarOffset);

    const culture = currentCulture(meta);
    setCulture(meta, culture, false);
    applyTheme(localStorage.getItem(THEME_KEY) || "light");

    const defaultCulture = meta.defaultCulture || "zh-Hans";
    if (culture !== defaultCulture) {
      setCulture(meta, culture, true);
    }

    // topbar height may settle after swagger paints
    setTimeout(syncTopbarOffset, 300);
    setTimeout(syncTopbarOffset, 1000);
  }

  function boot() {
    applyTheme(localStorage.getItem(THEME_KEY) || "light");

    fetch("/swagger/rcs-meta.json", { credentials: "same-origin" })
      .then((r) => (r.ok ? r.json() : Promise.reject(r.status)))
      .then((meta) => {
        const tryMount = () => {
          if (!document.body) return void setTimeout(tryMount, 50);
          mountToolbar(meta);
        };
        tryMount();
      })
      .catch(() => {
        mountToolbar({
          defaultCulture: "zh-Hans",
          cultures: [
            { id: "zh-Hans", label: "简体中文" },
            { id: "zh-Hant", label: "繁體中文" },
            { id: "en", label: "English" }
          ],
          groups: [
            { id: "abp", name: "ABP API" },
            { id: "rcs", name: "RCS API" },
            { id: "dashboard", name: "Dashboard API" },
            { id: "all", name: "All API" },
            { id: "common", name: "Common API" }
          ]
        });
      });
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", boot);
  } else {
    boot();
  }
})();
