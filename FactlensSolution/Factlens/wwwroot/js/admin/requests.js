// ============================================================
// admin/requests.js
// ============================================================
var currentPage = 1;
var PAGE_SIZE   = 50;

document.addEventListener("DOMContentLoaded", function() {
  if (!requireAdmin()) return;

  var user = Auth.getUser();
  var sidebarUser = document.getElementById("sidebarUser");
  if (sidebarUser) sidebarUser.textContent = (user && (user.fullName || user.username)) || "Admin";

  var debounce;
  document.getElementById("searchInput").addEventListener("input", function() {
    clearTimeout(debounce);
    debounce = setTimeout(function() { currentPage = 1; load(); }, 400);
  });
  document.getElementById("statusFilter").addEventListener("change", function() {
    currentPage = 1; load();
  });

  load();
});

async function load() {
  var search = document.getElementById("searchInput").value.trim();
  var status = document.getElementById("statusFilter").value;
  var params = new URLSearchParams({ page: currentPage, pageSize: PAGE_SIZE });
  if (search) params.set("search", search);
  if (status) params.set("status", status);

  var tbody = document.getElementById("requestsTable");
  tbody.innerHTML = '<tr><td colspan="9" style="text-align:center;padding:40px;">Loading…</td></tr>';

  try {
    var res  = await apiFetch("/api/admin/requests?" + params.toString());
    var data = await res.json();

    if (!data.data || !data.data.length) {
      tbody.innerHTML = '<tr><td colspan="9" style="text-align:center;padding:40px;color:var(--gray-400);">No records found.</td></tr>';
      document.getElementById("pagination").innerHTML = "";
      return;
    }

    tbody.innerHTML = data.data.map(function(r) {
      var statusClass = r.statusCode === 200 ? "status-200" : r.statusCode >= 500 ? "status-5xx" : "status-4xx";
      var cfg         = getVerdictConfig(r.verdict);
      var userId      = (r.userId || "—");
      var inputText   = (r.inputText || "").slice(0, 60);
      var errMsg      = (r.errorMessage || "").slice(0, 40);

      return "<tr>"
        + "<td>" + r.id + "</td>"
        + '<td class="td-truncate" style="max-width:100px;" title="' + escHtml(r.userId||"") + '">'
          + escHtml(userId.slice(0, 12)) + (userId.length > 12 ? "…" : "") + "</td>"
        + '<td class="td-truncate" title="' + escHtml(r.inputText||"") + '">'
          + escHtml(inputText) + (r.inputText && r.inputText.length > 60 ? "…" : "") + "</td>"
        + "<td>" + (r.verdict
            ? '<span class="verdict-pill" style="background:' + cfg.bg + ';color:' + cfg.color + ';font-size:11px;padding:3px 8px;">'
              + escHtml(r.verdict) + "</span>"
            : "—") + "</td>"
        + "<td>" + (r.confidenceScore != null ? r.confidenceScore : "—") + "</td>"
        + '<td><span class="status-badge ' + statusClass + '">' + r.statusCode + "</span></td>"
        + "<td>" + r.durationMs + "ms</td>"
        + '<td class="td-truncate" style="max-width:140px;color:#ef4444;" title="' + escHtml(r.errorMessage||"") + '">'
          + (errMsg ? escHtml(errMsg) + (r.errorMessage.length > 40 ? "…" : "") : "—") + "</td>"
        + '<td style="white-space:nowrap;">' + fmtDateTime(r.createdAt) + "</td>"
        + "</tr>";
    }).join("");

    renderPagination(data.totalCount || 0);

  } catch (err) {
    tbody.innerHTML = '<tr><td colspan="9" style="text-align:center;color:#ef4444;padding:40px;">'
      + "Failed to load: " + err.message + "</td></tr>";
  }
}

function renderPagination(total) {
  var pages = Math.ceil(total / PAGE_SIZE);
  var pag   = document.getElementById("pagination");
  if (pages <= 1) { pag.innerHTML = ""; return; }

  var html  = "";
  var start = Math.max(1, currentPage - 2);
  var end   = Math.min(pages, currentPage + 2);

  html += '<button class="page-btn" ' + (currentPage===1?"disabled":"") + ' onclick="goPage(' + (currentPage-1) + ')">‹</button>';
  if (start > 1) html += '<button class="page-btn" onclick="goPage(1)">1</button><span style="padding:0 6px;">…</span>';
  for (var i = start; i <= end; i++) {
    html += '<button class="page-btn ' + (i===currentPage?"active":"") + '" onclick="goPage(' + i + ')">' + i + '</button>';
  }
  if (end < pages) html += '<span style="padding:0 6px;">…</span><button class="page-btn" onclick="goPage(' + pages + ')">' + pages + '</button>';
  html += '<button class="page-btn" ' + (currentPage===pages?"disabled":"") + ' onclick="goPage(' + (currentPage+1) + ')">›</button>';

  pag.innerHTML = html;
}

function goPage(p) { currentPage = p; load(); window.scrollTo(0,0); }

function escHtml(s) {
  return String(s).replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;");
}
