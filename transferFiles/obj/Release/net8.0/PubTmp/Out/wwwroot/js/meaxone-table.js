/* ══════════════════════════════════════════════════════════════
   MEAX One — mo-table.js
   Filtros por columna + paginación para tablas .mo-table,
   sin dependencias. Funciona sobre HTML ya renderizado
   (server-side con Razor, o estático), así que sirve igual
   para tablas armadas en C#/Razor, PHP, Node, etc.

   MARCADO ESPERADO:
   <div class="mo-table-wrap" data-mo-table data-page-size="10">
     <table class="mo-table">
       <thead>
         <tr> ...encabezados normales... </tr>
         <tr class="mo-table-filter-row">
           <th><input class="mo-th-filter" data-mo-filter-col="0" placeholder="Filtrar…"></th>
           <th><select class="mo-th-filter" data-mo-filter-col="2"><option value="">Todos</option>...</select></th>
           <th></th> <!-- columna sin filtro: <th> vacío -->
         </tr>
       </thead>
       <tbody> ...filas... </tbody>
     </table>
   </div>
   <div class="mo-table-pagination" data-mo-pagination></div>

   No requiere llamar nada manualmente: se auto-inicializa en
   DOMContentLoaded sobre todos los [data-mo-table] de la página.
══════════════════════════════════════════════════════════════ */
(function () {
    'use strict';

    function initTable(wrap) {
        var table = wrap.querySelector('table.mo-table');
        if (!table) return;
        var tbody = table.querySelector('tbody');
        var allRows = Array.prototype.slice.call(tbody.querySelectorAll('tr'));
        var filterInputs = Array.prototype.slice.call(wrap.querySelectorAll('.mo-th-filter'));
        var paginationEl = wrap.nextElementSibling && wrap.nextElementSibling.hasAttribute('data-mo-pagination')
            ? wrap.nextElementSibling
            : wrap.parentElement.querySelector('[data-mo-pagination]');

        var pageSize = parseInt(wrap.getAttribute('data-page-size'), 10) || 10;
        var currentPage = 1;

        // Fila de "sin resultados" (se inserta una vez y se muestra/oculta)
        var emptyRow = document.createElement('tr');
        var colCount = table.querySelectorAll('thead tr:first-child th').length || 1;
        emptyRow.innerHTML = '<td class="mo-table-empty" colspan="' + colCount + '">No se encontraron resultados con esos filtros.</td>';
        emptyRow.style.display = 'none';
        tbody.appendChild(emptyRow);

        function rowMatchesFilters(row) {
            for (var i = 0; i < filterInputs.length; i++) {
                var input = filterInputs[i];
                var val = (input.value || '').trim().toLowerCase();
                if (!val) continue;
                var col = parseInt(input.getAttribute('data-mo-filter-col'), 10);
                var cell = row.children[col];
                var cellText = cell ? cell.textContent.trim().toLowerCase() : '';
                if (input.tagName === 'SELECT') {
                    if (cellText !== val) return false;
                } else {
                    if (cellText.indexOf(val) === -1) return false;
                }
            }
            return true;
        }

        function getFiltered() {
            return allRows.filter(rowMatchesFilters);
        }

        function render() {
            var filtered = getFiltered();
            var totalPages = Math.max(1, Math.ceil(filtered.length / pageSize));
            if (currentPage > totalPages) currentPage = totalPages;
            if (currentPage < 1) currentPage = 1;

            var start = (currentPage - 1) * pageSize;
            var end = start + pageSize;
            var pageRows = filtered.slice(start, end);

            allRows.forEach(function (r) { r.classList.add('mo-row-hidden'); });
            pageRows.forEach(function (r) { r.classList.remove('mo-row-hidden'); });

            emptyRow.style.display = filtered.length === 0 ? '' : 'none';

            if (paginationEl) renderPagination(paginationEl, filtered.length, totalPages);
        }

        function renderPagination(el, totalResults, totalPages) {
            var start = totalResults === 0 ? 0 : (currentPage - 1) * pageSize + 1;
            var end = Math.min(currentPage * pageSize, totalResults);

            el.innerHTML = '';

            var summary = document.createElement('div');
            summary.className = 'mo-pg-summary';
            summary.innerHTML = totalResults === 0
                ? 'Sin resultados'
                : 'Mostrando <b>' + start + '\u2013' + end + '</b> de <b>' + totalResults + '</b>';
            el.appendChild(summary);

            var right = document.createElement('div');
            right.className = 'mo-pg-right';

            // Selector de tamaño de página
            var sizeWrap = document.createElement('div');
            sizeWrap.className = 'mo-pg-size';
            var label = document.createElement('label');
            label.textContent = 'Filas por página';
            var select = document.createElement('select');
            [5, 10, 25, 50].forEach(function (n) {
                var opt = document.createElement('option');
                opt.value = n; opt.textContent = n;
                if (n === pageSize) opt.selected = true;
                select.appendChild(opt);
            });
            select.addEventListener('change', function () {
                pageSize = parseInt(select.value, 10);
                currentPage = 1;
                render();
            });
            sizeWrap.appendChild(label);
            sizeWrap.appendChild(select);
            right.appendChild(sizeWrap);

            // Botones de página
            var pagesWrap = document.createElement('div');
            pagesWrap.className = 'mo-pg-pages';

            function makeBtn(html, page, opts) {
                opts = opts || {};
                var btn = document.createElement('button');
                btn.type = 'button';
                btn.className = 'mo-pg-btn' + (opts.active ? ' active' : '');
                btn.innerHTML = html;
                btn.disabled = !!opts.disabled;
                btn.addEventListener('click', function () {
                    currentPage = page;
                    render();
                });
                return btn;
            }

            pagesWrap.appendChild(makeBtn('&lsaquo;', currentPage - 1, { disabled: currentPage <= 1 }));

            var pagesToShow = [];
            for (var p = 1; p <= totalPages; p++) {
                if (p === 1 || p === totalPages || Math.abs(p - currentPage) <= 1) pagesToShow.push(p);
            }
            var lastShown = 0;
            pagesToShow.forEach(function (p) {
                if (lastShown && p - lastShown > 1) {
                    var dots = document.createElement('span');
                    dots.className = 'mo-pg-ellipsis';
                    dots.textContent = '\u2026';
                    pagesWrap.appendChild(dots);
                }
                pagesWrap.appendChild(makeBtn(String(p), p, { active: p === currentPage }));
                lastShown = p;
            });

            pagesWrap.appendChild(makeBtn('&rsaquo;', currentPage + 1, { disabled: currentPage >= totalPages }));
            right.appendChild(pagesWrap);

            el.appendChild(right);
        }

        filterInputs.forEach(function (input) {
            var evt = input.tagName === 'SELECT' ? 'change' : 'input';
            input.addEventListener(evt, function () {
                currentPage = 1;
                render();
            });
        });

        render();
    }

    document.addEventListener('DOMContentLoaded', function () {
        Array.prototype.slice.call(document.querySelectorAll('[data-mo-table]')).forEach(initTable);
    });
})();
