// ============================================================
// admin-responsive.js — Mobile sidebar toggle for admin pages
// ============================================================
(function () {
    function initAdminResponsive() {
        var sidebar = document.querySelector(".admin-sidebar");
        var topbar = document.querySelector(".admin-topbar");
        if (!sidebar || !topbar) return;

        // Create overlay
        var overlay = document.createElement("div");
        overlay.className = "admin-overlay";
        overlay.id = "adminOverlay";
        document.body.appendChild(overlay);

        // Create menu button and inject into topbar
        var menuBtn = document.createElement("button");
        menuBtn.className = "admin-menu-btn";
        menuBtn.id = "adminMenuBtn";
        menuBtn.innerHTML = "☰";
        menuBtn.setAttribute("aria-label", "Open menu");
        topbar.insertBefore(menuBtn, topbar.firstChild);

        // Toggle
        menuBtn.addEventListener("click", function () {
            sidebar.classList.toggle("open");
            overlay.classList.toggle("open");
            document.body.style.overflow = sidebar.classList.contains("open") ? "hidden" : "";
        });

        // Close on overlay click
        overlay.addEventListener("click", function () {
            sidebar.classList.remove("open");
            overlay.classList.remove("open");
            document.body.style.overflow = "";
        });

        // Close when a sidebar link is clicked on mobile
        var links = sidebar.querySelectorAll(".sidebar-link");
        links.forEach(function (link) {
            link.addEventListener("click", function () {
                if (window.innerWidth <= 1024) {
                    sidebar.classList.remove("open");
                    overlay.classList.remove("open");
                    document.body.style.overflow = "";
                }
            });
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initAdminResponsive);
    } else {
        initAdminResponsive();
    }
})();