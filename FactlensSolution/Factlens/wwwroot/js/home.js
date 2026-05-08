// ============================================================
// home.js — Text/URL + Image tabs with loading overlay
// ============================================================
document.addEventListener("DOMContentLoaded", () => {
    renderNav("home");

    const newsInput = document.getElementById("newsInput");
    const charCount = document.getElementById("charCount");
    const errorMsg = document.getElementById("errorMsg");
    const analyzeBtn = document.getElementById("analyzeBtn");
    const btnText = document.getElementById("btnText");
    const cancelBtn = document.getElementById("cancelBtn");
    const urlBadge = document.getElementById("urlBadge");
    const imageInput = document.getElementById("imageInput");
    const uploadZone = document.getElementById("uploadZone");

    // ── Prefill from News-Feed "تحقق" button ──────────────────
    const prefill = sessionStorage.getItem("fl_prefill");
    if (prefill) {
        newsInput.value = prefill;
        charCount.textContent = prefill.length;
        sessionStorage.removeItem("fl_prefill");
    }

    // ── Live char count + URL detection ───────────────────────
    newsInput.addEventListener("input", () => {
        const val = newsInput.value.trim();
        charCount.textContent = newsInput.value.length;
        errorMsg.textContent = "";
        const isUrl = /^https?:\/\//i.test(val);
        urlBadge.style.display = isUrl ? "flex" : "none";
        newsInput.style.borderColor = isUrl ? "var(--navy)" : "";
        newsInput.style.background = isUrl ? "rgba(26,48,91,.03)" : "";
    });

    // ── Image file selection ───────────────────────────────────
    imageInput.addEventListener("change", () => {
        if (imageInput.files[0]) showPreview(imageInput.files[0]);
    });

    // ── Drag & Drop ────────────────────────────────────────────
    uploadZone.addEventListener("dragover", e => {
        e.preventDefault();
        uploadZone.classList.add("drag-over");
    });
    uploadZone.addEventListener("dragleave", () => {
        uploadZone.classList.remove("drag-over");
    });
    uploadZone.addEventListener("drop", e => {
        e.preventDefault();
        uploadZone.classList.remove("drag-over");
        const file = e.dataTransfer.files[0];
        if (file && file.type.startsWith("image/")) {
            showPreview(file);
            // sync to input for later FormData use
            const dt = new DataTransfer();
            dt.items.add(file);
            imageInput.files = dt.files;
        } else {
            showError("Please drop a valid image file (JPEG, PNG, WebP).");
        }
    });

    // ── Analyze button ─────────────────────────────────────────
    analyzeBtn.addEventListener("click", async () => {
        if (!Auth.isLoggedIn()) { window.location.href = "login.html"; return; }
        errorMsg.textContent = "";

        const activeTab = document.getElementById("tabText").classList.contains("active")
            ? "text"
            : "image";

        if (activeTab === "text") {
            await analyzeText();
        } else {
            await analyzeImage();
        }
    });

    cancelBtn.addEventListener("click", () => {
        newsInput.value = "";
        charCount.textContent = "0";
        cancelBtn.style.display = "none";
        hideOverlay();
    });

    // ──────────────────────────────────────────────────────────
    // TEXT / URL Analysis
    // ──────────────────────────────────────────────────────────
    async function analyzeText() {
        const text = newsInput.value.trim();
        if (!text) { showError("Please enter news text or a URL."); return; }
        const isUrl = /^https?:\/\//i.test(text);
        if (!isUrl && text.length < 10) { showError("Text is too short (min 10 chars)."); return; }

        setLoading(true, isUrl ? "Scraping & analyzing URL…" : "Cross-referencing sources…");

        try {
            const res = await apiFetch("/api/user/check-news", {
                method: "POST",
                body: JSON.stringify({ text })
            });
            const data = await res.json();
            if (!res.ok) throw new Error(data.error || "Analysis failed.");

            sessionStorage.setItem("fl_result", JSON.stringify(data));
            sessionStorage.setItem("fl_input_text", text);
            window.location.href = "result.html";
        } catch (err) {
            showError(err.message);
            setLoading(false);
        }
    }

    // ──────────────────────────────────────────────────────────
    // IMAGE Analysis
    // ──────────────────────────────────────────────────────────
    async function analyzeImage() {
        const file = imageInput.files[0];
        if (!file) { showError("Please select or drop an image first."); return; }

        const allowed = ["image/jpeg", "image/png", "image/webp"];
        if (!allowed.includes(file.type)) {
            showError("Only JPEG, PNG, and WebP images are supported.");
            return;
        }
        if (file.size > 10 * 1024 * 1024) {
            showError("Image is too large. Maximum size is 10 MB.");
            return;
        }

        setLoading(true, "Reading image with OCR…");

        try {
            const formData = new FormData();
            formData.append("file", file);

            // apiFetch adds Authorization header — but FormData needs NO Content-Type override
            const token = Auth.getToken ? Auth.getToken() : (localStorage.getItem("fl_token") || sessionStorage.getItem("fl_token"));
            const res = await fetch(BASE_URL + "/api/user/check-image", {
                method: "POST",
                headers: token ? { "Authorization": "Bearer " + token } : {},
                body: formData
            });

            const data = await res.json();
            if (!res.ok) throw new Error(data.error || "Image analysis failed.");

            // Store result — use filename as the "input text" label
            sessionStorage.setItem("fl_result", JSON.stringify(data));
            sessionStorage.setItem("fl_input_text", "📷 " + file.name);
            window.location.href = "result.html";
        } catch (err) {
            showError(err.message);
            setLoading(false);
        }
    }

    // ──────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────
    function showPreview(file) {
        const reader = new FileReader();
        reader.onload = e => {
            document.getElementById("previewImg").src = e.target.result;
            document.getElementById("previewName").textContent = file.name;
            document.getElementById("uploadIdle").style.display = "none";
            document.getElementById("uploadPreview").style.display = "flex";
        };
        reader.readAsDataURL(file);
        errorMsg.textContent = "";
    }

    function showError(msg) { errorMsg.textContent = msg; }

    function setLoading(on, subText) {
        analyzeBtn.disabled = on;
        btnText.textContent = on ? "Analyzing…" : "Analyze News";
        cancelBtn.style.display = on ? "inline-flex" : "none";

        if (on) {
            document.getElementById("analyzingSub").textContent = subText || "Cross-referencing sources…";
            showOverlay();
        } else {
            hideOverlay();
        }
    }

    function showOverlay() {
        const overlay = document.getElementById("analyzingOverlay");
        overlay.style.display = "flex";
        // Cycle sub-messages for UX
        const messages = [
            "Cross-referencing sources…",
            "Running AI verification…",
            "Checking trusted databases…",
            "Generating confidence score…"
        ];
        let i = 0;
        overlay._interval = setInterval(() => {
            i = (i + 1) % messages.length;
            document.getElementById("analyzingSub").textContent = messages[i];
        }, 2800);
    }

    function hideOverlay() {
        const overlay = document.getElementById("analyzingOverlay");
        overlay.style.display = "none";
        clearInterval(overlay._interval);
    }
});

// ── Tab switcher (global — called from onclick) ──────────────
function switchTab(tab) {
    const isText = tab === "text";
    document.getElementById("tabText").classList.toggle("active", isText);
    document.getElementById("tabImage").classList.toggle("active", !isText);
    document.getElementById("panelText").style.display = isText ? "block" : "none";
    document.getElementById("panelImage").style.display = isText ? "none" : "block";
    document.getElementById("errorMsg").textContent = "";

    // Update button label
    document.getElementById("btnText").textContent =
        isText ? "Analyze News" : "Analyze Image";
}

// ── Clear image preview (global — called from onclick) ───────
function clearImage() {
    document.getElementById("imageInput").value = "";
    document.getElementById("uploadIdle").style.display = "block";
    document.getElementById("uploadPreview").style.display = "none";
    document.getElementById("errorMsg").textContent = "";
}