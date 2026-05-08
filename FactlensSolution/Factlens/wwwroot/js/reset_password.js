// BASE_URL is defined in shared.js

document.addEventListener("DOMContentLoaded", function () {
    const params = new URLSearchParams(location.search);
    const token = params.get("token");
    const email = params.get("email");

    if (token && email) {
        // Show reset form — came from email link
        document.getElementById("forgotCard").style.display = "none";
        document.getElementById("resetCard").style.display = "block";
        handleResetForm(decodeURIComponent(email), decodeURIComponent(token));
    } else {
        handleForgotForm();
    }
});

// ── Forgot Password ─────────────────────────────────────
function handleForgotForm() {
    const form = document.getElementById("forgotForm");
    const btn = document.getElementById("forgotBtn");
    const errEl = document.getElementById("forgotError");
    const sucEl = document.getElementById("forgotSuccess");

    form.addEventListener("submit", async function (e) {
        e.preventDefault();
        errEl.textContent = "";
        sucEl.textContent = "";

        const email = document.getElementById("forgotEmail").value.trim();
        if (!email) { errEl.textContent = "Email required"; return; }

        btn.disabled = true;
        btn.textContent = "Sending…";

        try {
            const res = await fetch(BASE_URL + "/api/auth/forgot-password", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ email })
            });
            // البـ Backend بيرجع Ok() حتى لو الإيميل مش موجود (security)
            // فأي response = success من ناحية الـ UI
            if (res.ok || res.status < 500) {
                sucEl.textContent = "✅ If this email is registered, a reset link has been sent.";
            } else {
                errEl.textContent = "Server error (" + res.status + "). Please try again.";
            }
        } catch (err) {
            errEl.textContent = "Network error: " + err.message;
        } finally {
            btn.disabled = false;
            btn.textContent = "Send Reset Link";
        }
    });
}

// ── Reset Password ─────────────────────────────────────
function handleResetForm(email, token) {
    const form = document.getElementById("resetForm");
    const btn = document.getElementById("resetBtn");
    const errEl = document.getElementById("resetError");
    const sucEl = document.getElementById("resetSuccess");

    form.addEventListener("submit", async function (e) {
        e.preventDefault();
        errEl.textContent = "";
        sucEl.textContent = "";

        const newPw = document.getElementById("newPw").value;
        const confirmPw = document.getElementById("confirmPw").value;

        if (newPw !== confirmPw) { errEl.textContent = "Passwords do not match."; return; }
        if (newPw.length < 6) { errEl.textContent = "Password must be at least 6 characters."; return; }

        btn.disabled = true;
        btn.textContent = "Resetting…";

        try {
            const res = await fetch(BASE_URL + "/api/auth/reset-password", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    email,
                    token,
                    newPassword: newPw,
                    confirmPassword: confirmPw
                })
            });

            const data = await res.text().catch(() => "");
            if (!res.ok) throw new Error(data || "Reset failed. The link may have expired.");

            sucEl.textContent = "✅ Password reset successfully! Redirecting to sign in…";
            setTimeout(() => { window.location.href = "login.html"; }, 2500);
        } catch (err) {
            errEl.textContent = err.message;
        } finally {
            btn.disabled = false;
            btn.textContent = "Reset Password";
        }
    });
}

// ── Toggle password visibility ─────────────────────────
function togglePw(id, btn) {
    const inp = document.getElementById(id);
    const isText = inp.type === "text";
    inp.type = isText ? "password" : "text";
    btn.innerHTML = `<i class="fa-regular fa-eye${isText ? "" : "-slash"}"></i>`;
}