$(async function () {
  const res = await fetch('standby.json');
  const payload = await res.json();
  const headers = payload.headers;
  // Only rows that DON'T already have a Ticket ID (col index 15) belong on this page.
  const rows = payload.rows.filter(r => {
    const ticketId = (r.cells && r.cells[15] != null) ? String(r.cells[15]).trim() : '';
    return ticketId.length === 0;
  });

  // Add the Action column at the front
  const $thead = $('#headerRow');
  $thead.append('<th style="width:80px;">Action</th>');
  headers.forEach(h => $thead.append('<th>' + h + '</th>'));

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

  // Populate filter dropdowns from data
  const techs = new Set();
  const types = new Set();
  const approvals = new Set();
  rows.forEach(r => {
    if (r.cells[3]) techs.add(r.cells[3]);
    if (r.cells[8]) types.add(r.cells[8]);
    if (r.cells[13]) approvals.add(r.cells[13]);
  });
  const fill = ($sel, set) =>
    Array.from(set).sort().forEach(v => $sel.append(`<option value="${v}">${v}</option>`));
  fill($('#techFilter'), techs);
  fill($('#typeFilter'), types);
  fill($('#approvalFilter'), approvals);

  $('#loading').hide();
  $('#stockTable').show();

  const dt = $('#stockTable').DataTable({
    data: rows,
    columns: columns,
    pageLength: 25,
    lengthMenu: [5, 10, 25, 50, 100],
    order: [[1, 'desc']],
    deferRender: true,
    language: {
      info: 'Showing _START_ to _END_ of _TOTAL_ entries',
      search: 'Search:'
    }
  });

  // Action column is col 0, so user columns are shifted by +1
  $('#techFilter').on('change', function () {
    dt.column(4).search(this.value ? '^' + this.value + '$' : '', true, false).draw();
  });
  $('#typeFilter').on('change', function () {
    dt.column(9).search(this.value ? '^' + this.value + '$' : '', true, false).draw();
  });
  $('#approvalFilter').on('change', function () {
    dt.column(14).search(this.value ? '^' + this.value + '$' : '', true, false).draw();
  });
  $('#ticketFilter').on('keyup', function () {
    dt.column(16).search(this.value, false, false).draw();
  });

  // Push button → POST the row as Webhook 2 (Stock Transfer Request)
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

    // Source date is "YYYY-MM-DD HH:mm:ss"; convert to ISO by swapping the space.
    let isoDate = (c[2] || '').replace(' ', 'T');

    const payload = {
      RequestId:        c[0] || '',
      DocumentDateTime: isoDate,
      Technician:       c[3] || '',
      Part:             c[4] || '',
      qty:              parseFloat(c[6]) || 0,
      type:             c[8] || '',
      unit:             c[9] || '',
      approval:         c[13] || ''
    };

    $btn.prop('disabled', true);
    fetch(window.ATPAPI.base + '/api/stocktransfer', {
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

  window.resetAllFilters = function () {
    $('#techFilter, #typeFilter, #approvalFilter, #ticketFilter').val('');
    dt.search('').columns().search('').draw();
  };

  // ---- Bulk push: 5 IN + 5 OUT rows in sequence ----
  function buildTransferPayload(cells) {
    return {
      RequestId:        cells[0] || '',
      DocumentDateTime: (cells[2] || '').replace(' ', 'T'),
      Technician:       cells[3] || '',
      Part:             cells[4] || '',
      qty:              parseFloat(cells[6]) || 0,
      type:             cells[8] || '',
      unit:             cells[9] || '',
      approval:         cells[13] || ''
    };
  }

  async function pushOne(payload) {
    try {
      const r = await fetch(window.ATPAPI.base + '/api/stocktransfer', {
        method:  'POST',
        headers: { 'Content-Type': 'application/json', 'X-API-Key': window.ATPAPI.key },
        body:    JSON.stringify(payload)
      });
      const body = await r.json();
      return { ok: r.ok && body.success, status: body.status, msg: body.message || body.reason };
    } catch (e) {
      return { ok: false, msg: e.message };
    }
  }

  window.bulkPush5In5Out = async function () {
    const $btn = $('#btnBulkPush');
    if ($btn.prop('disabled')) return;
    $btn.prop('disabled', true);
    $btn.text('Pushing...');

    // Collect first 5 IN-type and first 5 OUT-type rows from the full dataset.
    const ins = rows.filter(r => (r.cells[8] || '').trim().toUpperCase() === 'IN').slice(0, 5);
    const outs = rows.filter(r => (r.cells[8] || '').trim().toUpperCase() === 'OUT').slice(0, 5);
    const batch = ins.concat(outs);
    if (batch.length === 0) {
      showToast('No IN/OUT rows found.', false);
      $btn.prop('disabled', false);
      $btn.text('Push 5 IN + 5 OUT');
      return;
    }

    let ok = 0, fail = 0;
    for (const row of batch) {
      const res = await pushOne(buildTransferPayload(row.cells));
      if (res.ok) ok++; else fail++;
    }
    showToast('Pushed: ' + ok + ' OK, ' + fail + ' failed.', fail === 0);
    $btn.prop('disabled', false);
    $btn.text('Push 5 IN + 5 OUT');
  };
});
