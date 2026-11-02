/* Quản lý sân thi đấu (FR15): GET/POST/PUT/DELETE /api/venues. */
App.main = function () {
  var $ = App.$;
  var venues = [];

  function fields(v) {
    v = v || {};
    return [
      { name: 'name', label: 'Tên sân', value: v.name || '', required: true },
      { name: 'address', label: 'Địa chỉ', value: v.address || '' },
      { name: 'capacity', label: 'Sức chứa (người)', type: 'number', min: 1, value: v.capacity || '' },
    ];
  }
  function body(v) {
    return { name: v.name.trim(), address: v.address.trim() || null, capacity: v.capacity ? +v.capacity : null };
  }

  function render() {
    if (!venues.length) { $('#venue-table').innerHTML = '<p class="muted">Chưa có sân nào.</p>'; return; }
    $('#venue-table').innerHTML = '<div class="table-scroll"><table class="tbl tbl-left"><thead><tr><th>Tên sân</th><th>Địa chỉ</th><th>Sức chứa</th><th><span class="sr-only">Thao tác</span></th></tr></thead><tbody>' +
      venues.map(function (v) {
        return '<tr><td style="font-weight:600">' + App.esc(v.name) + '</td><td>' + App.esc(v.address || '—') + '</td><td>' + (v.capacity || '—') + '</td>' +
          '<td class="al-right"><div class="cell-actions"><button class="btn btn-sm" data-act="edit" data-id="' + v.id + '">Sửa</button>' +
          '<button class="btn btn-sm btn-danger" data-act="delete" data-id="' + v.id + '">Xóa</button></div></td></tr>';
      }).join('') + '</tbody></table></div>';
  }

  async function load() {
    var res = await App.getAll('/api/venues');
    venues = res.ok ? res.data : [];
    render();
  }

  $('#add-venue').addEventListener('click', async function () {
    var v = await App.formDialog({ title: 'Thêm sân', fields: fields(), submitText: 'Thêm' });
    if (v && App.report(await App.post('/api/venues', body(v)))) load();
  });

  $('#venue-table').addEventListener('click', async function (e) {
    var btn = e.target.closest('[data-act]');
    if (!btn) return;
    var v = venues.filter(function (x) { return x.id === +btn.dataset.id; })[0];
    if (btn.dataset.act === 'edit') {
      var r = await App.formDialog({ title: 'Sửa sân', fields: fields(v) });
      if (r && App.report(await App.put('/api/venues/' + v.id, body(r)))) load();
    } else if (await App.confirm('Xóa sân ' + v.name + '?')) {
      if (App.report(await App.del('/api/venues/' + v.id))) load();
    }
  });

  load();
};
