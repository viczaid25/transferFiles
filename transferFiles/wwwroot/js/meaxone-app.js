/* Interacción del shell MEAX One (navbar + sidebar): idéntica a la de
   plantilla-meaxone.html. JS plano, sin dependencia de Blazor, porque
   #mo-navbar/#mo-sidebar viven en el layout estático (no interactivo). */
(function () {
    'use strict';

    window.moToggleSidebar = function () {
        var sb = document.getElementById('mo-sidebar');
        var main = document.getElementById('mo-main');
        if (!sb) return;
        if (window.innerWidth <= 768) {
            sb.classList.toggle('mobile-open');
        } else {
            sb.classList.toggle('collapsed');
            if (main) main.classList.toggle('sb-collapsed');
        }
    };

    window.moToggleUserMenu = function (e) {
        e.stopPropagation();
        var dd = document.getElementById('mo-user-dropdown');
        var ch = document.getElementById('mo-user-chevron');
        if (dd) dd.classList.toggle('open');
        if (ch) ch.classList.toggle('rotated');
    };

    document.addEventListener('click', function (e) {
        var wrap = document.getElementById('mo-user-wrap');
        if (wrap && !wrap.contains(e.target)) {
            var dd = document.getElementById('mo-user-dropdown');
            var ch = document.getElementById('mo-user-chevron');
            if (dd) dd.classList.remove('open');
            if (ch) ch.classList.remove('rotated');
        }
    });
})();
