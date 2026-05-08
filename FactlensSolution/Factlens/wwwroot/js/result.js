// ============================================================
// result.js
// Two entry points:
//   1. Fresh analysis  → sessionStorage "fl_result"
//   2. From history    → ?historyId=N  → GET /api/user/history (find by id)
// ============================================================
var currentRecordId = null;
var helpfulValue = null;
var ratingValue = null;

document.addEventListener("DOMContentLoaded", async function () {
    if (!requireAuth()) return;
    renderNav("result");

    var params = new URLSearchParams(location.search);
    var historyId = params.get("historyId");

    if (historyId) {
        await loadFromHistory(parseInt(historyId, 10));
    } else {
        var raw = sessionStorage.getItem("fl_result");
        if (!raw) {
            showToast("No result found. Redirecting…", "warning");
            setTimeout(function () { location.href = "home.html"; }, 2000);
            return;
        }
        var result = JSON.parse(raw);
        var inputText = sessionStorage.getItem("fl_input_text") || "";
        renderResult(result, inputText);
        fetchLatestRecordId();
    }

    initTabs();
    initFeedback();
    document.getElementById("shareBtn").addEventListener("click", toggleShare);
});

// ── Load from history by ID ───────────────────────────────
async function loadFromHistory(id) {
    showLoadingState();
    try {
        var res = await apiFetch("/api/user/history?page=1&pageSize=200");
        if (!res.ok) throw new Error("HTTP " + res.status);
        var body = await res.json();

        var items = body.data || [];
        var item = null;
        for (var i = 0; i < items.length; i++) {
            if (items[i].id === id) { item = items[i]; break; }
        }
        if (!item) throw new Error("Record not found.");

        currentRecordId = item.id;

        renderResult({
            verdict: item.verdict,
            confidence_score: item.confidenceScore,
            explanation: item.explanation,
            top_sources: item.topSources || {}
        }, item.searchText);

        if (item.shareId) {
            document.getElementById("shareUrl").value = buildShareUrl(item.shareId);
        }
    } catch (err) {
        showErrorState(err.message);
    }
}

function showLoadingState() {
    document.getElementById("scoreNum").textContent = "…";
    document.getElementById("verdictPill").textContent = "Loading…";
    document.getElementById("explanationContent").innerHTML =
        '<div class="loading-center"><div class="spinner"></div><span>Fetching result…</span></div>';
}

function showErrorState(msg) {
    document.getElementById("verdictPill").textContent = "Error";
    document.getElementById("claimText").style.display = "none";
    document.getElementById("explanationContent").innerHTML =
        '<div class="empty-state"><div class="empty-icon">⚠️</div>'
        + '<h3>Could not load result</h3><p>' + msg + '</p>'
        + '<a href="history.html" class="btn-primary" style="margin-top:16px;display:inline-flex;">← Back to History</a>'
        + '</div>';
}

// ── Render ────────────────────────────────────────────────
function renderResult(r, inputText) {
    var cfg = getVerdictConfig(r.verdict);
    var score = r.confidence_score != null ? r.confidence_score : (r.confidenceScore != null ? r.confidenceScore : 0);

    // Score circle
    animateNumber(document.getElementById("scoreNum"), 0, score, 1200);
    var circ = document.getElementById("circleProgress");
    if (circ) {
        var C = 2 * Math.PI * 90;
        circ.style.strokeDasharray = C;
        circ.style.strokeDashoffset = C;
        circ.style.stroke = cfg.color;
        circ.style.transition = "stroke-dashoffset 1.4s cubic-bezier(.4,0,.2,1), stroke .3s";
        setTimeout(function () { circ.style.strokeDashoffset = C - (score / 100) * C; }, 150);
    }

    // Verdict pill
    var pill = document.getElementById("verdictPill");
    pill.textContent = cfg.icon + " " + cfg.label;
    pill.style.background = cfg.bg;
    pill.style.color = cfg.color;
    pill.style.border = "1.5px solid " + cfg.color + "44";

    // Claim
    var claimEl = document.getElementById("claimText");
    if (inputText) {
        claimEl.textContent = '"' + inputText.slice(0, 220) + (inputText.length > 220 ? "…" : "") + '"';
        claimEl.style.display = "block";
    } else {
        claimEl.style.display = "none";
    }

    // Explanation
    var expEl = document.getElementById("explanationContent");
    var explanation = r.explanation || "No explanation available.";

    var scrapeFailures = [
        "failed to scrape",
        "no evidence found",
        "parsing error"
    ];
    var isScrapeError = scrapeFailures.some(function (k) {
        return explanation.toLowerCase().indexOf(k) !== -1;
    });

    if (isScrapeError) {
        expEl.innerHTML =
            '<div style="text-align:center;padding:32px 20px;">' +
            '<div style="font-size:48px;margin-bottom:16px;">🔗</div>' +
            '<h3 style="font-family:var(--font-display);color:var(--navy);margin-bottom:10px;">Could not access this URL</h3>' +
            '<p style="color:var(--gray-500);font-size:15px;line-height:1.7;max-width:480px;margin:0 auto 20px;">' +
            'This website may be blocking automated access, or the link may be expired / invalid.' +
            '</p>' +
            '<div style="background:rgba(205,167,85,.10);border:1.5px solid rgba(205,167,85,.30);border-radius:12px;padding:16px 20px;max-width:420px;margin:0 auto;text-align:left;">' +
            '<p style="font-size:13px;font-weight:700;color:var(--navy);margin-bottom:8px;">💡 Try instead:</p>' +
            '<ul style="font-size:14px;color:var(--gray-700);line-height:1.9;padding-left:18px;margin:0;">' +
            '<li>Copy and paste the article <strong>text</strong> directly</li>' +
            '<li>Use a different news source URL</li>' +
            '<li>Try the article headline as a text claim</li>' +
            '</ul></div></div>';
    } else {
        var paras = explanation.split(/\n\n+/).filter(Boolean);
        expEl.innerHTML = paras.length
            ? paras.map(function (p) { return "<p>" + p.trim() + "</p>"; }).join("")
            : "<p>" + explanation + "</p>";
    }

    // Sources
    var srcEl = document.getElementById("sourcesContent");
    var sources = r.top_sources || r.topSources || {};
    var entries = Object.entries(sources);
    if (!entries.length) {
        srcEl.innerHTML = '<div class="empty-state"><div class="empty-icon">🔗</div><p>No sources returned for this analysis.</p></div>';
    } else {
        srcEl.innerHTML = entries.map(function (e) {
            var name = e[0], url = e[1];
            return '<div class="source-item">'
                + '<div class="source-info"><h4>' + escHtml(name) + '</h4>'
                + '<a href="' + escHtml(url) + '" target="_blank" rel="noopener">' + escHtml(url) + '</a></div>'
                + '<span class="verdict-pill" style="background:rgba(34,197,94,.12);color:#22c55e;font-size:12px;padding:4px 10px;">Source</span>'
                + '</div>';
        }).join("");
    }
}

// ── Fetch latest record ID (fresh analysis) ───────────────
async function fetchLatestRecordId() {
    try {
        var res = await apiFetch("/api/user/history?page=1&pageSize=1");
        if (!res.ok) return;
        var body = await res.json();
        var items = body.data || [];
        if (items.length) {
            currentRecordId = items[0].id;
            if (items[0].shareId) {
                document.getElementById("shareUrl").value = buildShareUrl(items[0].shareId);
            }
        }
    } catch { /* silent */ }
}

function buildShareUrl(shareId) {
    return location.origin + location.pathname.replace("result.html", "shared-result.html") + "?shareId=" + shareId;
}

// ── Share ─────────────────────────────────────────────────
function toggleShare() {
    var box = document.getElementById("shareBox");
    box.style.display = box.style.display === "none" ? "block" : "none";
}

function copyShare() {
    var url = document.getElementById("shareUrl").value;
    if (!url) { showToast("Share link not ready yet.", "warning"); return; }
    navigator.clipboard.writeText(url)
        .then(function () { showToast("Link copied!", "success"); })
        .catch(function () {
            document.getElementById("shareUrl").select();
            document.execCommand("copy");
            showToast("Copied!", "success");
        });
}

// ── Download Image via public API ─────────────────────────
async function downloadFile(type) {
    var shareId = getShareIdFromUrl();
    if (!shareId) {
        showToast("Share link not ready yet — please wait a moment and try again.", "warning");
        return;
    }

    var btn = document.getElementById("downloadImageBtn");
    var originalText = btn.textContent;
    btn.disabled = true;
    btn.textContent = "Preparing…";

    var endpoint = BASE_URL + "/api/public/share/" + encodeURIComponent(shareId) + "/image";
    var filename = "factlens-result.png";
    var expectedMime = "image/png";

    try {
        var res = await fetch(endpoint);

        if (!res.ok) {
            var errText = await res.text().catch(function () { return ""; });
            throw new Error(errText || "Server error (" + res.status + ")");
        }

        var contentType = res.headers.get("content-type") || "";
        if (!contentType.includes(expectedMime) && !contentType.includes("octet-stream")) {
            throw new Error("Unexpected response from server. Please try again.");
        }

        var blob = await res.blob();

        if (blob.size === 0) {
            throw new Error("Downloaded file is empty. Please try again.");
        }

        var a = document.createElement("a");
        a.href = URL.createObjectURL(blob);
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(a.href);
        showToast("Image downloaded!", "success");

    } catch (err) {
        showToast("Download failed: " + err.message, "error");
    } finally {
        btn.disabled = false;
        btn.textContent = originalText;
    }
}

function getShareIdFromUrl() {
    var input = document.getElementById("shareUrl");
    if (!input || !input.value) return null;
    var parts = input.value.split("shareId=");
    return parts.length > 1 ? parts[1] : null;
}

// ── Tabs ──────────────────────────────────────────────────
function initTabs() {
    document.querySelectorAll(".tab-btn").forEach(function (btn) {
        btn.addEventListener("click", function () {
            document.querySelectorAll(".tab-btn").forEach(function (b) { b.classList.remove("active"); });
            document.querySelectorAll(".tab-panel").forEach(function (p) { p.classList.remove("active"); });
            btn.classList.add("active");
            document.getElementById("tab-" + btn.dataset.tab).classList.add("active");
        });
    });
}

// ── Feedback ──────────────────────────────────────────────
function initFeedback() {
    document.querySelectorAll(".star").forEach(function (star) {
        star.addEventListener("click", function () {
            ratingValue = parseInt(star.dataset.v, 10);
            document.querySelectorAll(".star").forEach(function (s) {
                s.classList.toggle("active", parseInt(s.dataset.v, 10) <= ratingValue);
            });
        });
    });
    document.getElementById("submitFeedback").addEventListener("click", submitFeedback);
}

function setHelpful(val) {
    helpfulValue = val;
    document.getElementById("helpfulYes").classList.toggle("selected", val === true);
    document.getElementById("helpfulNo").classList.toggle("selected", val === false);
}

async function submitFeedback() {
    if (!currentRecordId) {
        showToast("Cannot submit — result ID not available yet.", "error");
        return;
    }
    if (helpfulValue === null) {
        showToast("Please select helpful or not helpful first.", "warning");
        return;
    }

    var btn = document.getElementById("submitFeedback");
    var msg = document.getElementById("feedbackMsg");
    var comment = document.getElementById("feedbackComment").value.trim();
    var report = document.getElementById("reportIncorrect").checked;

    btn.disabled = true;
    btn.textContent = "Submitting…";
    msg.style.color = "#22c55e";
    msg.textContent = "";

    try {
        var res = await apiFetch("/api/user/feedback", {
            method: "POST",
            body: JSON.stringify({
                searchRecordId: currentRecordId,
                helpful: helpfulValue,
                rating: ratingValue,
                reportIncorrect: report,
                comment: comment
            })
        });

        var data = await res.json().catch(function () { return {}; });
        if (!res.ok) {
            var errMsg = typeof data === "string" ? data : (data.message || "Submission failed.");
            throw new Error(errMsg);
        }

        msg.textContent = "✅ Thank you for your feedback!";
        btn.style.display = "none";
    } catch (err) {
        msg.style.color = "#ef4444";
        msg.textContent = err.message;
        btn.disabled = false;
        btn.textContent = "Submit Feedback";
    }
}

function escHtml(s) {
    return String(s).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
}