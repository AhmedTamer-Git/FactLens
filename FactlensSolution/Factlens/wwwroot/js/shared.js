// ============================================================
// shared.js — Config · Auth · Utils · NavBar
// ============================================================

const BASE_URL = "";

// ─── Verdict Config ───────────────────────────────────────
const VERDICT_CONFIG = {
    "Verified True": { color: "#22c55e", bg: "rgba(34,197,94,.12)", label: "Verified True", icon: "✅" },
    "Misleading": { color: "#f59e0b", bg: "rgba(245,158,11,.12)", label: "Misleading", icon: "⚠️" },
    "False": { color: "#ef4444", bg: "rgba(239,68,68,.12)", label: "False", icon: "❌" },
    "unverified": { color: "#9ca3af", bg: "rgba(156,163,175,.12)", label: "Unverified", icon: "❓" },
};

// ─── Mapping: AI verdicts → Frontend verdicts ─────────────
// الـ AI بيبعت: True / False / Misleading / Unverified
// الفرونت بيتوقع: Verified True / False / Misleading / Unverifiable
const VERDICT_MAP = {
    "true": "Verified True",
    "verified true": "Verified True",
    "misleading": "Misleading",
    "false": "False",
    "unverified": "unverified",
};

function normalizeVerdict(verdict) {
    if (!verdict) return null;
    return VERDICT_MAP[verdict.toLowerCase().trim()] || verdict;
}

function getVerdictConfig(verdict) {
    if (!verdict) return { color: "#9ca3af", bg: "rgba(156,163,175,.12)", label: "Unknown", icon: "—" };
    const normalized = normalizeVerdict(verdict);
    const key = Object.keys(VERDICT_CONFIG).find(k => k.toLowerCase() === normalized.toLowerCase());
    return VERDICT_CONFIG[key] || { color: "#9ca3af", bg: "rgba(156,163,175,.12)", label: normalized, icon: "—" };
}

// ─── Auth ─────────────────────────────────────────────────
const Auth = {
    getToken() {
        return localStorage.getItem("fl_token") || sessionStorage.getItem("fl_token");
    },
    getUser() {
        try {
            const raw = localStorage.getItem("fl_user") || sessionStorage.getItem("fl_user");
            return raw ? JSON.parse(raw) : null;
        } catch { return null; }
    },
    isLoggedIn() { return !!this.getToken(); },

    // ── Decode JWT and check Role claim ──
    isAdmin() {
        const token = this.getToken();
        if (!token) return false;
        try {
            // JWT payload is the middle part (base64url encoded)
            const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
            const payload = JSON.parse(atob(base64));

            // .NET Identity uses the full URI claim key for roles
            const roleClaim =
                payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ||
                payload["role"] ||
                payload["roles"] ||
                [];

            const rolesArr = Array.isArray(roleClaim) ? roleClaim : [roleClaim];
            return rolesArr.some(r => r === "Admin");
        } catch { return false; }
    },

    // ── Save token + user, pick storage based on remember ──
    save(token, user, remember = false) {
        // Always clear both storages first to avoid stale data
        this._clear();
        const store = remember ? localStorage : sessionStorage;
        store.setItem("fl_token", token);
        store.setItem("fl_user", JSON.stringify(user));
    },

    _clear() {
        ["fl_token", "fl_user"].forEach(k => {
            localStorage.removeItem(k);
            sessionStorage.removeItem(k);
        });
    },

    logout() {
        this._clear();
        // Always redirect to login regardless of current depth
        const root = _rootPath();
        window.location.href = root + "html/login.html";
    }
};

// ─── Path helper (internal) ───────────────────────────────
// Figures out relative path back to project root
// Works for: /html/page.html  and  /html/admin/page.html
function _rootPath() {
    const path = location.pathname;
    if (path.includes("/html/admin/")) return "../../";  // html/admin/*.html
    if (path.includes("/html/")) return "../";     // html/*.html
    return "./";
}

// ─── Guards ───────────────────────────────────────────────
function requireAuth() {
    if (!Auth.isLoggedIn()) {
        window.location.href = _rootPath() + "html/login.html";
        return false;
    }
    return true;
}

function requireAdmin() {
    if (!Auth.isLoggedIn()) {
        window.location.href = _rootPath() + "html/login.html";
        return false;
    }
    if (!Auth.isAdmin()) {
        window.location.href = _rootPath() + "html/home.html";
        return false;
    }
    return true;
}

// ─── API fetch helper ─────────────────────────────────────
async function apiFetch(path, options = {}) {
    const token = Auth.getToken();
    const headers = {
        "Content-Type": "application/json",
        ...(token ? { "Authorization": "Bearer " + token } : {}),
        ...(options.headers || {})
    };
    const res = await fetch(BASE_URL + path, { ...options, headers });
    if (res.status === 401) {
        Auth.logout();
        throw new Error("Session expired. Please log in again.");
    }
    return res;
}

// ─── Nav ──────────────────────────────────────────────────
function renderNav(activePage) {
    const nav = document.getElementById("main-nav");
    if (!nav) return;

    const user = Auth.getUser();
    const root = _rootPath();

    const isLoggedIn = Auth.isLoggedIn();
    const links = [
        { key: "home", href: root + "html/home.html", label: "Home", show: true },
        { key: "history", href: root + "html/history.html", label: "History", show: isLoggedIn },
        { key: "News-Feed", href: root + "html/News-Feed.html", label: "News", show: true },
        { key: "about", href: root + "html/about.html", label: "About Us", show: true },
    ];

    const visibleLinks = links.filter(l => l.show);
    const adminLink = Auth.isAdmin()
        ? `<a href="${root}html/admin/dashboard.html" class="nav-link nav-admin-link">⚙ Admin</a>`
        : "";
    const adminDrawerLink = Auth.isAdmin()
        ? `<a href="${root}html/admin/dashboard.html" class="nav-admin-link">⚙ Admin</a>`
        : "";

    nav.innerHTML = `
    <div class="nav-inner">
      <a class="nav-brand" href="${root}html/home.html">
        <span class="brand-icon">⬡</span>
        <span>Fact<strong>Lens</strong></span>
      </a>
      <div class="nav-links" style="flex:1;justify-content:center;">
        ${visibleLinks.map(l =>
        `<a href="${l.href}" class="nav-link ${activePage === l.key ? 'active' : ''}">${l.label}</a>`
    ).join("")}
        ${adminLink}
      </div>
      <div class="nav-actions">
        ${user ? `
          <div class="nav-profile-wrap" id="navProfileWrap">
            <button class="nav-profile-btn" onclick="_toggleProfileDropdown(event)" id="navProfileBtn">
              <div class="nav-avatar">${(user.fullName || user.username || "U")[0].toUpperCase()}</div>
              <span class="nav-username">${user.fullName || user.username || ""}</span>
              <svg class="nav-chevron" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="6 9 12 15 18 9"/></svg>
            </button>
            <div class="nav-profile-dropdown" id="navProfileDropdown">
              <div class="nav-dropdown-header">
                <div class="nav-dropdown-avatar">${(user.fullName || user.username || "U")[0].toUpperCase()}</div>
                <div>
                  <div class="nav-dropdown-name">${user.fullName || user.username || ""}</div>
                  <div class="nav-dropdown-email">${user.email || ""}</div>
                </div>
              </div>
              <div class="nav-dropdown-divider"></div>
              <a href="${root}html/profile.html" class="nav-dropdown-item">
                <span class="nav-dropdown-icon">👤</span> My Profile
              </a>
              <a href="${root}html/history.html" class="nav-dropdown-item">
                <span class="nav-dropdown-icon">📋</span> History
              </a>
              <div class="nav-dropdown-divider"></div>
              <button class="nav-dropdown-item nav-dropdown-logout" onclick="Auth.logout()">
                <span class="nav-dropdown-icon">🚪</span> Sign Out
              </button>
            </div>
          </div>
        ` : `
          <a href="${root}html/login.html"   class="nav-btn nav-signin">Sign In</a>
          <a href="${root}html/sign_up.html" class="nav-btn nav-signup">Sign Up</a>
        `}
        <button class="nav-hamburger" id="navHamburger" aria-label="Menu" onclick="_toggleNavDrawer()">
          <span></span><span></span><span></span>
        </button>
      </div>
    </div>

    <!-- Mobile Drawer Overlay -->
    <div class="nav-drawer" id="navDrawer" onclick="_closeNavDrawerIfOutside(event)">
      <div class="nav-drawer-inner">
        <button class="nav-drawer-close" onclick="_closeNavDrawer()">✕</button>
        <div class="nav-drawer-brand">Fact<strong>Lens</strong></div>
        <div class="nav-drawer-links">
          ${visibleLinks.map(l =>
        `<a href="${l.href}" class="${activePage === l.key ? 'active' : ''}">${l.label}</a>`
    ).join("")}
          ${adminDrawerLink}
        </div>
        <div class="nav-drawer-actions">
          ${user ? `
            <div class="nav-drawer-user">
              <div class="nav-avatar">${(user.fullName || user.username || "U")[0].toUpperCase()}</div>
              <span class="nav-drawer-username">${user.fullName || user.username || ""}</span>
            </div>
            <button class="nav-btn nav-logout" style="width:100%;text-align:center;" onclick="Auth.logout()">Logout</button>
          ` : `
            <a href="${root}html/login.html"   class="nav-btn nav-signin" style="text-align:center;">Sign In</a>
            <a href="${root}html/sign_up.html" class="nav-btn nav-signup" style="text-align:center;">Sign Up</a>
          `}
        </div>
      </div>
    </div>`;
}

// ─── Profile Dropdown ────────────────────────────────────
function _toggleProfileDropdown(e) {
    e.stopPropagation();
    var dropdown = document.getElementById("navProfileDropdown");
    var btn = document.getElementById("navProfileBtn");
    if (!dropdown) return;
    var isOpen = dropdown.classList.toggle("open");
    btn.classList.toggle("open", isOpen);
    // اقفل لما يضغط برا
    if (isOpen) {
        setTimeout(function () {
            document.addEventListener("click", _closeProfileDropdown, { once: true });
        }, 0);
    }
}
function _closeProfileDropdown() {
    var dropdown = document.getElementById("navProfileDropdown");
    var btn = document.getElementById("navProfileBtn");
    if (dropdown) dropdown.classList.remove("open");
    if (btn) btn.classList.remove("open");
}

function _toggleNavDrawer() {
    var drawer = document.getElementById("navDrawer");
    var hamburger = document.getElementById("navHamburger");
    if (!drawer) return;

    // Move drawer to <body> on first use so it escapes the sticky nav stacking context
    if (drawer.parentElement !== document.body) {
        document.body.appendChild(drawer);
    }

    var isOpen = drawer.classList.toggle("open");
    if (hamburger) hamburger.classList.toggle("open", isOpen);
    document.body.style.overflow = isOpen ? "hidden" : "";
}
function _closeNavDrawer() {
    const drawer = document.getElementById("navDrawer");
    const hamburger = document.getElementById("navHamburger");
    if (drawer) drawer.classList.remove("open");
    if (hamburger) hamburger.classList.remove("open");
    document.body.style.overflow = "";
}
function _closeNavDrawerIfOutside(e) {
    if (e.target === document.getElementById("navDrawer")) _closeNavDrawer();
}

// ─── Toast ────────────────────────────────────────────────
function showToast(message, type) {
    type = type || "info";
    const colors = { info: "#3b82f6", success: "#22c55e", error: "#ef4444", warning: "#f59e0b" };
    let t = document.getElementById("fl-toast");
    if (!t) {
        t = document.createElement("div");
        t.id = "fl-toast";
        t.style.cssText = [
            "position:fixed", "bottom:28px", "right:28px", "padding:14px 22px",
            "border-radius:12px", "color:#fff", "font-size:14px", "font-weight:500",
            "z-index:9999", "opacity:0", "transform:translateY(16px)",
            "transition:all .3s ease", "pointer-events:none",
            "box-shadow:0 8px 32px rgba(0,0,0,.2)", "max-width:320px",
            "font-family:'DM Sans',sans-serif"
        ].join(";");
        document.body.appendChild(t);
    }
    t.style.background = colors[type] || colors.info;
    t.textContent = message;
    t.style.opacity = "1";
    t.style.transform = "translateY(0)";
    clearTimeout(t._t);
    t._t = setTimeout(function () {
        t.style.opacity = "0";
        t.style.transform = "translateY(16px)";
    }, 3500);
}

// ─── Animate Number ──────────────────────────────────────
function animateNumber(el, from, to, ms) {
    if (!el) return;
    ms = ms || 1200;
    var start = performance.now();
    function run(now) {
        var p = Math.min((now - start) / ms, 1);
        var ease = 1 - Math.pow(1 - p, 3);
        el.textContent = Math.round(from + (to - from) * ease);
        if (p < 1) requestAnimationFrame(run);
    }
    requestAnimationFrame(run);
}

// ─── Format Date ─────────────────────────────────────────
function fmtDate(iso) {
    return new Date(iso).toLocaleDateString("en-US", {
        year: "numeric", month: "short", day: "numeric"
    });
}
function fmtDateTime(iso) {
    return new Date(iso).toLocaleString("en-US", {
        year: "numeric", month: "short", day: "numeric",
        hour: "2-digit", minute: "2-digit"
    });
}

// ─── Google OAuth callback handler ───────────────────────
// Backend redirects back with ?token=... in the URL
// This runs on page load on any page and picks up the token
(function handleGoogleCallback() {
    // Only run on google-callback page — avoid hijacking ?token= on other pages
    // (e.g. confirm-email.html and reset_password.html also use ?token=)
    if (!location.pathname.includes("google-callback")) return;
    const params = new URLSearchParams(location.search);
    const token = params.get("token");
    if (!token) return;

    // Decode payload to get user info
    try {
        const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
        const payload = JSON.parse(atob(base64));

        const email = payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"]
            || payload["email"] || "";
        const name = payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"]
            || payload["name"] || email;

        Auth.save(token, { email, fullName: name, username: name }, true);
    } catch {
        // Even if decode fails, save the token so the user is logged in
        Auth.save(token, { email: "", fullName: "User", username: "User" }, true);
    }

    // Clean the URL and redirect
    const cleanUrl = location.pathname;
    history.replaceState({}, "", cleanUrl);

    // Redirect admin → dashboard, user → home
    if (Auth.isAdmin()) {
        window.location.href = _rootPath() + "html/admin/dashboard.html";
    } else {
        window.location.href = _rootPath() + "html/home.html";
    }
})();