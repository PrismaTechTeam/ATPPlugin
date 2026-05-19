$(async function () {
  const res = await fetch('data.json');
  const payload = await res.json();
  const headers = payload.headers;
  const rows = payload.rows;

  // Build header row with extra "Action" column at the start
  const $thead = $('#headerRow');
  $thead.append('<th style="width:80px;">Action</th>');
  headers.forEach(h => $thead.append('<th>' + h + '</th>'));

  // Build column definitions for DataTables
  const columns = [];
  columns.push({
    data: null,
    orderable: false,
    searchable: false,
    render: (data, type, row) =>
      '<button class="btn-push" data-id="' + (row.id || '') + '">Push</button>'
  });
  for (let i = 0; i < headers.length; i++) {
    columns.push({ data: 'cells.' + i });
  }

  // Populate technician dropdown from the Description column (col 5: "Stock Issue-XXX")
  const techs = new Set();
  rows.forEach(r => {
    const desc = r.cells[5] || '';
    const m = desc.match(/Stock Issue-(.+)$/);
    if (m) techs.add(m[1].trim());
  });
  const $tech = $('#techFilter');
  Array.from(techs).sort().forEach(t => $tech.append(`<option value="${t}">${t}</option>`));

  $('#loading').hide();
  $('#stockTable').show();

  const dt = $('#stockTable').DataTable({
    data: rows,
    columns: columns,
    pageLength: 25,
    lengthMenu: [5, 10, 25, 50, 100],
    order: [[4, 'desc']],
    deferRender: true,
    language: {
      info: 'Showing _START_ to _END_ of _TOTAL_ entries',
      search: 'Search:'
    }
  });

  // Technician filter
  $('#techFilter').on('change', function () {
    const val = this.value;
    dt.column(6).search(val ? 'Stock Issue-' + val : '', false, false).draw();
  });

  // Item Code filter (col 12 = Item Code in original headers; +1 because Action is col 0)
  $('#itemCodeFilter').on('keyup', function () {
    dt.column(12).search(this.value, false, false).draw();
  });

  // Serial filter (col 13 in original = Serial No)
  $('#serialFilter').on('keyup', function () {
    dt.column(13).search(this.value, false, false).draw();
  });

  // Push button → POST the row as Webhook 1 (Stock Issue Request)
  $('#stockTable tbody').on('click', '.btn-push', function (e) {
    e.preventDefault();
    const $btn = $(this);
    if ($btn.prop('disabled')) return;

    const row = dt.row($btn.closest('tr')).data();
    if (!row || !row.cells) {
      showToast('Could not read row data', false);
      return;
    }
    const c = row.cells;

    // dd/MM/yyyy → ISO yyyy-MM-ddT00:00:00
    let isoDate = '';
    const m = (c[0] || '').match(/^(\d{2})\/(\d{2})\/(\d{4})$/);
    if (m) isoDate = m[3] + '-' + m[2] + '-' + m[1] + 'T00:00:00';

    let technician = c[7] || '';
    const tm = (c[5] || '').match(/Stock Issue-(.+)$/);
    if (tm) technician = tm[1].trim();

    const payload = {
      StockIssueId:  c[3] || '',
      IssueDateTime: isoDate,
      StockIssueNo:  c[3] || '',
      ReferenceNo:   c[4] || '',
      Description:   c[5] || '',
      Department:    c[6] || '',
      Job:           c[7] || '',
      Technician:    technician,
      Location:      c[8] || '',
      ItemCode:      c[11] || '',
      Quantity:      parseFloat(c[14]) || 0,
      UOM:           c[16] || ''
    };

    $btn.prop('disabled', true);
    fetch(window.ATPAPI.base + '/api/stockissue', {
      method:  'POST',
      headers: { 'Content-Type': 'application/json', 'X-API-Key': window.ATPAPI.key },
      body:    JSON.stringify(payload)
    })
    .then(r => r.json().then(b => ({ ok: r.ok, body: b })))
    .then(({ ok, body }) => {
      if (ok && body.success) showToast(body.message + ' (' + body.status + ')', true);
      else showToast('Push failed: ' + (body.reason || body.errorCode || 'unknown'), false);
    })
    .catch(err => showToast('Push failed: ' + err.message, false))
    .finally(() => $btn.prop('disabled', false));
  });

  window.applyDateFilter = function () {
    const from = $('#dateFrom').val();
    const to = $('#dateTo').val();
    $.fn.dataTable.ext.search = $.fn.dataTable.ext.search.filter(fn => fn._isDateFilter !== true);
    if (from || to) {
      const fn = function (settings, data) {
        const cell = data[1]; // Issue Date is column 1 (after Action col 0)
        if (!cell) return true;
        const parts = cell.split('/'); // dd/mm/yyyy
        if (parts.length !== 3) return true;
        const d = new Date(+parts[2], +parts[1] - 1, +parts[0]);
        if (from && d < new Date(from)) return false;
        if (to && d > new Date(to)) return false;
        return true;
      };
      fn._isDateFilter = true;
      $.fn.dataTable.ext.search.push(fn);
    }
    dt.draw();
  };

  window.resetAllFilters = function () {
    $('#techFilter, #itemCodeFilter, #serialFilter, #dateFrom, #dateTo').val('');
    dt.search('').columns().search('').draw();
    $.fn.dataTable.ext.search = $.fn.dataTable.ext.search.filter(fn => fn._isDateFilter !== true);
    dt.draw();
  };

  // ---- Bulk push: first 10 rows on the current DataTables page (after filters) ----
  function buildIssuePayload(c) {
    let isoDate = '';
    const m = (c[0] || '').match(/^(\d{2})\/(\d{2})\/(\d{4})$/);
    if (m) isoDate = m[3] + '-' + m[2] + '-' + m[1] + 'T00:00:00';
    let technician = c[7] || '';
    const tm = (c[5] || '').match(/Stock Issue-(.+)$/);
    if (tm) technician = tm[1].trim();
    return {
      StockIssueId:  c[3] || '',
      IssueDateTime: isoDate,
      StockIssueNo:  c[3] || '',
      ReferenceNo:   c[4] || '',
      Description:   c[5] || '',
      Department:    c[6] || '',
      Job:           c[7] || '',
      Technician:    technician,
      Location:      c[8] || '',
      ItemCode:      c[11] || '',
      Quantity:      parseFloat(c[14]) || 0,
      UOM:           c[16] || ''
    };
  }

  async function pushOne(payload) {
    try {
      const r = await fetch(window.ATPAPI.base + '/api/stockissue', {
        method:  'POST',
        headers: { 'Content-Type': 'application/json', 'X-API-Key': window.ATPAPI.key },
        body:    JSON.stringify(payload)
      });
      const body = await r.json();
      return r.ok && body.success;
    } catch (e) {
      return false;
    }
  }

  window.pushInvalidPayload = async function () {
    const $btn = $('#btnPushError');
    if ($btn.prop('disabled')) return;
    $btn.prop('disabled', true);
    const original = $btn.text();
    $btn.text('Pushing...');

    // Deliberately broken payload: missing required fields (StockIssueId, IssueDateTime, etc.),
    // wrong type on Quantity. API should respond 400 INVALID_PAYLOAD and write an Error log row.
    const badPayload = {
      StockIssueNo: 'BAD-' + Date.now(),
      Description:  'Intentional bad payload for error-log test',
      Quantity:     'not-a-number'
    };

    try {
      const r = await fetch(window.ATPAPI.base + '/api/stockissue', {
        method:  'POST',
        headers: { 'Content-Type': 'application/json', 'X-API-Key': window.ATPAPI.key },
        body:    JSON.stringify(badPayload)
      });
      const body = await r.json().catch(() => ({}));
      if (r.ok && body.success) {
        showToast('Unexpected success — API accepted bad payload', false);
      } else {
        showToast('Error pushed (' + r.status + ' ' + (body.errorCode || 'ERROR') + '). Check View Log.', true);
      }
    } catch (e) {
      showToast('Push failed: ' + e.message, false);
    } finally {
      $btn.prop('disabled', false);
      $btn.text(original);
    }
  };

  window.bulkPushCurrentPage = async function () {
    const $btn = $('#btnBulkPush');
    if ($btn.prop('disabled')) return;
    $btn.prop('disabled', true);
    $btn.text('Pushing...');

    // Rows on the *current page* honouring sort + filter, capped at 10.
    const pageRows = dt.rows({ page: 'current', search: 'applied' }).data().toArray().slice(0, 10);
    if (pageRows.length === 0) {
      showToast('No rows on the current page.', false);
      $btn.prop('disabled', false);
      $btn.text('Push 10 from current page');
      return;
    }

    let ok = 0, fail = 0;
    for (const row of pageRows) {
      const success = await pushOne(buildIssuePayload(row.cells));
      if (success) ok++; else fail++;
    }
    showToast('Pushed: ' + ok + ' OK, ' + fail + ' failed.', fail === 0);
    $btn.prop('disabled', false);
    $btn.text('Push 10 from current page');
  };
});
