// ============================================================
// history.js
// ============================================================
var currentPage = 1;
var PAGE_SIZE = 10;

document.addEventListener("DOMContentLoaded", async function () {
    if (!requireAuth()) return;
    renderNav("history");

    var searchInput = document.getElementById("searchInput");
    var verdictFilter = document.getElementById("verdictFilter");
    var clearAllBtn = document.getElementById("clearAllBtn");

    var debounce;
    searchInput.addEventListener("input", function () {
        clearTimeout(debounce);
        debounce = setTimeout(function () { currentPage = 1; loadHistory(); }, 400);
    });
    verdictFilter.addEventListener("change", function () { currentPage = 1; loadHistory(); });

    clearAllBtn.addEventListener("click", async function () {
        if (!confirm("Clear ALL history? This cannot be undone.")) return;
        try {
            // ✅ /clear must be called BEFORE /{id} routes - backend handles it
            var res = await apiFetch("/api/user/history/clear", { method: "DELETE" });
            if (!res.ok) throw new Error("Failed");
            showToast("History cleared.", "success");
            loadHistory();
        } catch { showToast("Failed to clear history.", "error"); }
    });

    loadHistory();
});

async function loadHistory() {
    var search = document.getElementById("searchInput").value.trim();
    var verdict = document.getElementById("verdictFilter").value;
    var list = document.getElementById("historyList");

    list.innerHTML = '<div class="loading-center"><div class="spinner"></div><span>Loading…</span></div>';

    // ✅ Match Swagger params exactly:
    // search (string), verdict (string), from (DateTime), to (DateTime), page (int), pageSize (int)
    var params = new URLSearchParams({ page: currentPage, pageSize: PAGE_SIZE });
    if (search) params.set("search", search);
    if (verdict) params.set("verdict", verdict);

    try {
        var res = await apiFetch("/api/user/history?" + params.toString());
        if (!res.ok) throw new Error("HTTP " + res.status);
        var body = await res.json();

        // ✅ Backend returns { totalCount, page, pageSize, data: [...] }
        var items = body.data || [];
        var total = body.totalCount || 0;

        renderCards(items);
        renderPagination(total);
    } catch (err) {
        list.innerHTML = '<div class="empty-state"><div class="empty-icon">⚠️</div>'
            + '<h3>Failed to load history</h3><p>' + err.message + '</p></div>';
    }
}

function renderCards(items) {
    var list = document.getElementById("historyList");
    if (!items.length) {
        list.innerHTML = '<div class="empty-state">'
            + '<div class="empty-icon">📋</div>'
            + '<h3>No history yet</h3>'
            + '<p>Start by analyzing a news article on the home page.</p>'
            + '</div>';
        return;
    }

    list.innerHTML = items.map(function (item) {
        var cfg = getVerdictConfig(normalizeVerdict(item.verdict));
        var score = item.confidenceScore != null ? item.confidenceScore : 0;
        var deg = Math.round((score / 100) * 360);
        return '<div class="history-card">'
            + '<div class="hist-circle" style="--ring-color:' + cfg.color + ';background:conic-gradient(' + cfg.color + ' ' + deg + 'deg,#e5e7eb ' + deg + 'deg)">'
            + '<div class="hist-inner">'
            + '<span class="hist-score" style="color:' + cfg.color + '">' + score + '</span>'
            + '<span class="hist-label">Credibility</span>'
            + '</div></div>'
            + '<div class="hist-middle">'
            + '<div class="hist-title">' + escHtml(item.searchText) + '</div>'
            + '<div class="hist-meta">'
            + '<span class="verdict-pill" style="background:' + cfg.bg + ';color:' + cfg.color + ';font-size:12px;padding:4px 12px;">'
            + cfg.icon + ' ' + cfg.label + '</span>'
            + '<span class="hist-date">📅 ' + fmtDate(item.searchTime) + '</span>'
            + '</div></div>'
            + '<div class="hist-actions">'
            + '<button class="hist-view-btn" onclick="viewResult(' + item.id + ')">👁 View Result</button>'
            + '<button class="hist-del-btn"  onclick="deleteRecord(' + item.id + ')">🗑 Delete</button>'
            + '</div></div>';
    }).join("");
}

function renderPagination(total) {
    var pages = Math.ceil(total / PAGE_SIZE);
    var pag = document.getElementById("pagination");
    if (pages <= 1) { pag.innerHTML = ""; return; }

    var html = '<button class="page-btn" ' + (currentPage === 1 ? "disabled" : "") + ' onclick="goPage(' + (currentPage - 1) + ')">‹</button>';
    for (var i = 1; i <= pages; i++) {
        html += '<button class="page-btn ' + (i === currentPage ? "active" : "") + '" onclick="goPage(' + i + ')">' + i + '</button>';
    }
    html += '<button class="page-btn" ' + (currentPage === pages ? "disabled" : "") + ' onclick="goPage(' + (currentPage + 1) + ')">›</button>';
    pag.innerHTML = html;
}

function goPage(p) { currentPage = p; loadHistory(); window.scrollTo(0, 0); }

function viewResult(id) {
    window.location.href = "result.html?historyId=" + id;
}

async function deleteRecord(id) {
    if (!confirm("Delete this record?")) return;
    try {
        // ✅ DELETE /api/user/history/{id}  — integer id
        var res = await apiFetch("/api/user/history/" + id, { method: "DELETE" });
        if (!res.ok) throw new Error("Failed");
        showToast("Deleted.", "success");
        loadHistory();
    } catch { showToast("Failed to delete.", "error"); }
}

function escHtml(s) {
    return String(s).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
}