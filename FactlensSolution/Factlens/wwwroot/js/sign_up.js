// ============================================================
// sign_up.js  — matches RegisterDto exactly:
//   Username, Email, FullName, Age, Phone, Password, ConfirmPassword
// ============================================================
document.addEventListener("DOMContentLoaded", function () {

    document.getElementById("signupForm").addEventListener("submit", async function (e) {
        e.preventDefault();

        var fullname = document.getElementById("fullname").value.trim();
        var username = document.getElementById("username").value.trim();
        var email = document.getElementById("email").value.trim();
        var phone = document.getElementById("phone").value.trim();
        var age = parseInt(document.getElementById("age").value) || 0;
        var password = document.getElementById("password").value;
        var confirmPassword = document.getElementById("confirmPassword").value;
        var terms = document.getElementById("terms").checked;

        var btn = document.getElementById("signupBtn");
        var errEl = document.getElementById("signupError");
        var sucEl = document.getElementById("signupSuccess");
        errEl.textContent = "";
        sucEl.textContent = "";

        // ── Client-side validation ──
        if (!fullname || !username || !email || !password || !confirmPassword) {
            errEl.textContent = "All required fields must be filled in.";
            return;
        }
        if (!terms) {
            errEl.textContent = "You must agree to the Terms of Service.";
            return;
        }
        if (password !== confirmPassword) {
            errEl.textContent = "Passwords do not match.";
            return;
        }
        if (age < 13 || age > 120) {
            errEl.textContent = "Please enter a valid age (13–120).";
            return;
        }

        btn.disabled = true;
        btn.textContent = "Creating account…";

        try {
            var res = await fetch(BASE_URL + "/api/auth/register", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    username: username,
                    email: email,
                    fullName: fullname,
                    age: age,
                    phone: phone || "",
                    password: password,
                    confirmPassword: confirmPassword    // ← required by RegisterDto
                })
            });

            // Backend can return string, object, or array of identity errors
            var data = await res.json().catch(function () { return {}; });

            if (!res.ok) {
                var msg = "";
                if (typeof data === "string") {
                    msg = data;
                } else if (Array.isArray(data)) {
                    // ASP.NET Identity errors array: [{code, description}]
                    msg = data.map(function (e) { return e.description || e; }).join(" ");
                } else {
                    msg = data.message || data.title || "Registration failed.";
                }
                throw new Error(msg);
            }

            document.getElementById("signupForm").reset();
            window.location.href = "check-email.html";س

        } catch (err) {
            errEl.textContent = err.message;
        } finally {
            btn.disabled = false;
            btn.textContent = "Create Account";
        }
    });
});

function togglePw(id, btn) {
    var inp = document.getElementById(id);
    var isText = inp.type === "text";
    inp.type = isText ? "password" : "text";
    btn.innerHTML = '<i class="fa-regular fa-eye' + (isText ? "" : "-slash") + '"></i>';
}