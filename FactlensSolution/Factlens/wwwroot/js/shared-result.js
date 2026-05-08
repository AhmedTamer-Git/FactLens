// ============================================================
// shared-result.js — Public share page
// ✅ GET /api/public/share/{shareId}        → SharedResultDto
// ✅ GET /api/public/share/{shareId}/image  → file download
// ============================================================
document.addEventListener("DOMContentLoaded", async function () {
    var params = new URLSearchParams(location.search);
    var shareId = params.get("shareId");
    var content = document.getElementById("sharedContent");

    if (!shareId) {
        content.innerHTML = '<div class="empty-state"><div class="empty-icon">❌</div>'
            + '<h3>Invalid Link</h3><p>This share link is invalid or expired.</p></div>';
        return;
    }

    try {
        var res = await fetch(BASE_URL + "/api/public/share/" + encodeURIComponent(shareId));

        if (!res.ok) throw new Error("Not found");

        var data = await res.json();

        var cfg = getVerdictConfig(data.verdict);
        var score = data.confidence != null ? data.confidence : 0;

        // ✅ sources is Dictionary<string,string>
        var sources = data.sources || {};
        var sourceEntries = Object.entries(sources);

        content.innerHTML =
            // Export buttons — image only
            '<div class="export-bar">'
            + '<button class="btn-primary" onclick="downloadSharedFile(\'' + encodeURIComponent(shareId) + '\',\'image\')">🖼 Download Image</button>'
            + '<a href="home.html" class="btn-outline">⬡ Try FactLens</a>'
            + '</div>'

            // Score card
            + '<div class="score-card" style="margin-bottom:24px;">'
            + '<div class="score-visual"><div class="circle-wrap">'
            + '<svg viewBox="0 0 220 220" width="200" height="200">'
            + '<circle class="circle-bg" cx="110" cy="110" r="90"/>'
            + '<circle class="circle-prog" id="circleProgress" cx="110" cy="110" r="90"/>'
            + '</svg>'
            + '<div class="circle-label"><span id="scoreNum">0</span><small>% Credibility</small></div>'
            + '</div></div>'

            + '<div class="score-info">'
            + '<div class="verdict-pill verdict-lg" style="background:' + cfg.bg + ';color:' + cfg.color + ';border:1.5px solid ' + cfg.color + '44;margin-bottom:16px;">'
            + cfg.icon + ' ' + cfg.label + '</div>'
            + '<div class="claim-text">&ldquo;' + escHtml(data.claim || "") + '&rdquo;</div>'
            + '<p style="font-size:13px;color:var(--gray-400);margin-top:12px;">Analyzed on ' + fmtDateTime(data.time) + '</p>'
            + '</div></div>'

            // Explanation
            + '<div class="tab-panel active" style="margin-bottom:20px;">'
            + '<h3 style="font-family:\'Playfair Display\',serif;font-size:20px;color:var(--navy);margin-bottom:16px;">AI Explanation</h3>'
            + '<div class="explanation-text">'
            + (data.explanation || "No explanation available.").split(/\n\n+/).filter(Boolean)
                .map(function (p) { return "<p>" + p + "</p>"; }).join("")
            + '</div></div>'

            // Sources
            + (sourceEntries.length
                ? '<div class="tab-panel active" style="margin-bottom:40px;">'
                + '<h3 style="font-family:\'Playfair Display\',serif;font-size:20px;color:var(--navy);margin-bottom:16px;">Evidence Sources</h3>'
                + sourceEntries.map(function (e) {
                    return '<div class="source-item">'
                        + '<div class="source-info"><h4>' + escHtml(e[0]) + '</h4>'
                        + '<a href="' + escHtml(e[1]) + '" target="_blank" rel="noopener">' + escHtml(e[1]) + '</a></div>'
                        + '</div>';
                }).join("")
                + '</div>'
                : "")

            + '<div style="text-align:center;padding:40px;opacity:.4;font-size:13px;">Powered by <strong>FactLens AI</strong></div>';

        // Animate circle
        var circ = document.getElementById("circleProgress");
        var C = 2 * Math.PI * 90;
        circ.style.strokeDasharray = C;
        circ.style.strokeDashoffset = C;
        circ.style.stroke = cfg.color;
        circ.style.transition = "stroke-dashoffset 1.4s cubic-bezier(.4,0,.2,1)";
        setTimeout(function () { circ.style.strokeDashoffset = C - (score / 100) * C; }, 150);
        animateNumber(document.getElementById("scoreNum"), 0, score, 1200);

    } catch (err) {
        content.innerHTML = '<div class="empty-state"><div class="empty-icon">🔗</div>'
            + '<h3>Result Not Found</h3>'
            + '<p>This link may be expired or the result was deleted.</p>'
            + '<a href="home.html" class="btn-primary" style="margin-top:20px;display:inline-flex;">← Try FactLens</a>'
            + '</div>';
    }
});

function escHtml(s) {
    return String(s).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
}

async function downloadSharedFile(shareId, type) {
    var btns = document.querySelectorAll(".export-bar button");
    btns.forEach(function (b) { b.disabled = true; });

    var endpoint = BASE_URL + "/api/public/share/" + shareId + "/image";
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
        if (blob.size === 0) throw new Error("Downloaded file is empty.");

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
        btns.forEach(function (b) { b.disabled = false; });
    }
}