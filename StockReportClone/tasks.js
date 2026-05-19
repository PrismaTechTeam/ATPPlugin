$(async function () {
  const res = await fetch('tasks.json');
  const payload = await res.json();
  const headers = payload.headers;
  const rows = payload.rows;

  const $thead = $('#headerRow');
  headers.forEach(h => $thead.append('<th>' + h + '</th>'));

  const columns = [];
  for (let i = 0; i < headers.length; i++) {
    (function (idx) {
      columns.push({
        data: null,
        render: function (row) {
          if (!row || !row.cells) return '';
          const v = row.cells[idx];
          return (v === undefined || v === null) ? '' : v;
        }
      });
    })(i);
  }

  // Populate filter dropdowns
  const techs = new Set(), statuses = new Set(), zones = new Set(), recurring = new Set();
  rows.forEach(r => {
    if (r.cells[7]) techs.add(r.cells[7]);
    if (r.cells[2]) statuses.add(r.cells[2]);
    if (r.cells[1]) zones.add(r.cells[1]);
    if (r.cells[8]) recurring.add(r.cells[8]);
  });
  const fill = ($sel, set) =>
    Array.from(set).sort().forEach(v => $sel.append(`<option value="${v}">${v}</option>`));
  fill($('#techFilter'),      techs);
  fill($('#statusFilter'),    statuses);
  fill($('#zoneFilter'),      zones);
  fill($('#recurringFilter'), recurring);

  $('#loading').hide();
  $('#stockTable').show();

  const dt = $('#stockTable').DataTable({
    data: rows,
    columns: columns,
    pageLength: 25,
    lengthMenu: [10, 25, 50, 100],
    order: [[6, 'desc']],
    deferRender: true,
    language: { info: 'Showing _START_ to _END_ of _TOTAL_ entries', search: 'Search:' }
  });

  $('#techFilter').on('change', function () {
    dt.column(7).search(this.value ? '^' + this.value + '$' : '', true, false).draw();
  });
  $('#statusFilter').on('change', function () {
    dt.column(2).search(this.value ? '^' + this.value + '$' : '', true, false).draw();
  });
  $('#zoneFilter').on('change', function () {
    dt.column(1).search(this.value ? '^' + this.value + '$' : '', true, false).draw();
  });
  $('#recurringFilter').on('change', function () {
    dt.column(8).search(this.value ? '^' + this.value + '$' : '', true, false).draw();
  });
  $('#trackingFilter').on('keyup', function () {
    dt.column(0).search(this.value, false, false).draw();
  });

  window.resetAllFilters = function () {
    $('#techFilter, #statusFilter, #zoneFilter, #recurringFilter, #trackingFilter').val('');
    dt.search('').columns().search('').draw();
  };
});
