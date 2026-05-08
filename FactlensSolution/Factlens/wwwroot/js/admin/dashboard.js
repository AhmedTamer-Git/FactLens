// ============================================================
// admin/dashboard.js
// ============================================================
document.addEventListener("DOMContentLoaded", async function() {
  // Guard — must be admin
  if (!requireAdmin()) return;

  // Show logged-in user name in sidebar
  var user = Auth.getUser();
  var sidebarUser = document.getElementById("sidebarUser");
  if (sidebarUser) {
    sidebarUser.textContent = (user && (user.fullName || user.username)) || "Admin";
  }

  var lastUpdated = document.getElementById("lastUpdated");
  if (lastUpdated) {
    lastUpdated.textContent = "Updated: " + new Date().toLocaleTimeString();
  }

  // Load all sections in parallel
  await Promise.all([
    loadSummary(),
    loadVerdicts(),
    loadRecentRequests()
  ]);
});

// ── Summary stats ────────────────────────────────────────
async function loadSummary() {
  try {
    var res  = await apiFetch("/api/admin/summary");
    var data = await res.json();

    document.getElementById("statsGrid").innerHTML = [
      { label: "Total Requests",  value: data.totalRequests,          sub: "All time",        cls: "" },
      { label: "Today",           value: data.requestsToday,          sub: "Requests today",  cls: "" },
      { label: "Last 7 Days",     value: data.requestsLast7Days,      sub: "Requests",        cls: "" },
      { label: "Errors (7d)",     value: data.errorsLast7Days,        sub: "Status ≥ 400",    cls: "danger" },
      { label: "Avg Response",    value: data.avgResponseMsLast7Days + "<small style='font-size:18px;'>ms</small>",
                                                                       sub: "Last 7 days",     cls: "success" },
    ].map(function(s) {
      return '<div class="stat-card ' + s.cls + '">'
        + '<div class="stat-label">' + s.label + '</div>'
        + '<div class="stat-value">' + s.value + '</div>'
        + '<div class="stat-sub">'   + s.sub   + '</div>'
        + '</div>';
    }).join("");

    var errorRate = data.requestsLast7Days > 0
      ? ((data.errorsLast7Days / data.requestsLast7Days) * 100).toFixed(1)
      : 0;

    document.getElementById("quickStats").innerHTML =
      "📈 <b>Today:</b> " + data.requestsToday + "<br>" +
      "📊 <b>Last 7 days:</b> " + data.requestsLast7Days + "<br>" +
      "⚡ <b>Avg response:</b> " + data.avgResponseMsLast7Days + "ms<br>" +
      "❌ <b>Error rate (7d):</b> " + errorRate + "%<br>" +
      "🔢 <b>Total ever:</b> " + data.totalRequests.toLocaleString();

  } catch (err) {
    document.getElementById("statsGrid").innerHTML =
      '<div class="stat-card danger"><div class="stat-label">Failed to load stats</div>'
      + '<div class="stat-sub">' + err.message + '</div></div>';
  }
}

// ── Verdict donut chart ───────────────────────────────────
async function loadVerdicts() {
  try {
    var res  = await apiFetch("/api/admin/verdicts");
    var data = await res.json();

    var COLORS = {
        "True": "#22c55e",
        "Misleading":    "#f59e0b",
        "False":         "#ef4444",
        "Unverified":  "#9ca3af"
    };

    var labels = data.map(function(d) { return d.verdict; });
    var counts = data.map(function(d) { return d.count; });
    var colors = data.map(function(d) { return COLORS[d.verdict] || "#6b7280"; });

    new Chart(document.getElementById("verdictChart"), {
      type: "doughnut",
      data: {
        labels: labels,
        datasets: [{
          data: counts,
          backgroundColor: colors,
          borderWidth: 3,
          borderColor: "#ffffff"
        }]
      },
      options: {
        responsive: true,
        plugins: {
          legend: {
            position: "bottom",
            labels: { font: { family: "'DM Sans', sans-serif", size: 13 }, padding: 16 }
          },
          tooltip: {
            callbacks: {
              label: function(ctx) {
                var total = ctx.dataset.data.reduce(function(a,b){return a+b;}, 0);
                var pct   = total > 0 ? ((ctx.parsed / total) * 100).toFixed(1) : 0;
                return " " + ctx.label + ": " + ctx.parsed + " (" + pct + "%)";
              }
            }
          }
        }
      }
    });
  } catch { /* chart is non-critical */ }
}

// ── Recent requests table ─────────────────────────────────
async function loadRecentRequests() {
  var tbody = document.getElementById("recentTable");
  try {
    var res  = await apiFetch("/api/admin/requests?page=1&pageSize=8");
    var data = await res.json();

    if (!data.data || !data.data.length) {
      tbody.innerHTML = '<tr><td colspan="7" style="text-align:center;padding:40px;color:var(--gray-400);">No requests yet.</td></tr>';
      return;
    }

    tbody.innerHTML = data.data.map(function(r) {
      var statusClass = r.statusCode === 200 ? "status-200" : r.statusCode >= 500 ? "status-5xx" : "status-4xx";
      var cfg         = getVerdictConfig(r.verdict);
      return "<tr>"
        + "<td>" + r.id + "</td>"
        + '<td class="td-truncate">' + escHtml(r.inputText || "") + "</td>"
        + "<td>" + (r.verdict
            ? '<span class="verdict-pill" style="background:' + cfg.bg + ';color:' + cfg.color + ';font-size:12px;padding:3px 10px;">'
              + cfg.icon + " " + r.verdict + "</span>"
            : "—") + "</td>"
        + "<td>" + (r.confidenceScore != null ? r.confidenceScore : "—") + "</td>"
        + '<td><span class="status-badge ' + statusClass + '">' + r.statusCode + "</span></td>"
        + "<td>" + r.durationMs + "ms</td>"
        + "<td>" + fmtDateTime(r.createdAt) + "</td>"
        + "</tr>";
    }).join("");

  } catch (err) {
    tbody.innerHTML = '<tr><td colspan="7" style="text-align:center;color:#ef4444;padding:40px;">'
      + "Failed to load: " + err.message + "</td></tr>";
  }
}

function escHtml(s) {
  return String(s)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;");
}
