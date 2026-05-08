// ============================================================
// login.js
// ============================================================
document.addEventListener("DOMContentLoaded", function () {
    if (Auth.isLoggedIn()) {
        window.location.href = Auth.isAdmin() ? "admin/dashboard.html" : "home.html";
        return;
    }

    document.getElementById("loginForm").addEventListener("submit", async function (e) {
        e.preventDefault();

        var username = document.getElementById("usernameField").value.trim();
        var password = document.getElementById("passwordField").value;
        var remember = document.getElementById("rememberMe").checked;
        var btn = document.getElementById("loginBtn");
        var errEl = document.getElementById("loginError");

        errEl.innerHTML = "";
        btn.disabled = true;
        btn.textContent = "Signing in…";

        try {
            var res = await fetch(BASE_URL + "/api/auth/login", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ username: username, password: password })
            });

            // ✅ الباك دايماً بيرجع JSON دلوقتي
            var data = await res.json();

            if (!res.ok) {
                var msg = data.message || data.title || "Invalid credentials.";
                var code = data.code || "";

                // ✅ حالة خاصة: إيميل مش متأكد
                if (code === "EMAIL_NOT_CONFIRMED") {
                    showEmailNotConfirmed(errEl, username);
                    return;
                }

                throw new Error(msg);
            }

            Auth.save(data.token, {
                email: data.email || "",
                username: data.username || username,
                fullName: data.fullName || data.username || username
            }, remember);

            window.location.href = Auth.isAdmin()
                ? "admin/dashboard.html"
                : "home.html";

        } catch (err) {
            errEl.innerHTML = '<span class="err-text">⚠️ ' + escHtml(err.message) + '</span>';
        } finally {
            btn.disabled = false;
            btn.textContent = "Sign In";
        }
    });

    // ── Google OAuth ──
    document.getElementById("googleBtn").addEventListener("click", function () {
        var returnUrl = window.location.origin
            + window.location.pathname.replace("login.html", "google-callback.html");
        window.location.href = BASE_URL + "/api/externalauth/google?returnUrl="
            + encodeURIComponent(returnUrl);
    });
});

// ── Email not confirmed UI ────────────────────────────────────
function showEmailNotConfirmed(errEl, username) {
    errEl.innerHTML =
        '<div class="confirm-notice">'
        + '<div class="confirm-notice-icon">✉️</div>'
        + '<div class="confirm-notice-body">'
        + '<p class="confirm-notice-title">Email not confirmed yet</p>'
        + '<p class="confirm-notice-sub">We sent a confirmation link to your inbox. Please check your email and click the link to activate your account.</p>'
        + '<a href="check-email.html" class="confirm-notice-btn">Go to confirmation page →</a>'
        + '</div>'
        + '</div>';
}

function escHtml(s) {
    return String(s)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;");
}

function togglePw(id, btn) {
    var inp = document.getElementById(id);
    var isText = inp.type === "text";
    inp.type = isText ? "password" : "text";
    btn.innerHTML = '<i class="fa-regular fa-eye' + (isText ? "" : "-slash") + '"></i>';
}