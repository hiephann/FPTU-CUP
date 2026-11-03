/* Quản lý giải đấu (FR05, FR06, FR10): danh sách, tạo, sửa, xóa, chuyển trạng thái. */
App.main = function () {
  var $ = App.$;
  var STEPS = ['Draft', 'Registration', 'Ongoing', 'Finished'];
  var items = [];

  function stepper(status) {
    var idx = STEPS.indexOf(status);
    return '<ol class="stepper">' + STEPS.map(function (s, i) {
      return '<li class="' + (i < idx ? 'is-done' : i === idx ? 'is-current' : '') + '"><span class="dot"></span>' + App.TOURNAMENT_STATUS[s] + '</li>';
    }).join('') + '</ol>';
  }

  function render() {
    if (!items.length) { $('#tournament-table').innerHTML = '<p class="muted">Chưa có giải đấu nào.</p>'; return; }
    $('#tournament-table').innerHTML = '<div class="table-scroll"><table class="tbl tbl-left"><thead><tr><th>Giải đấu</th><th>Thời gian</th><th>Đội</th><th>Số lượt</th><th>Trạng thái</th><th><span class="sr-only">Thao tác</span></th></tr></thead><tbody>' +
      items.map(function (t) {
        var idx = STEPS.indexOf(t.status);
        var next = STEPS[idx + 1];
        return '<tr><td><b>' + App.esc(t.name) + '</b><br><span class="note">' + App.esc(t.season) + '</span></td>' +
          '<td class="wrap">' + App.dmy(t.startDate) + ' – ' + App.dmy(t.endDate) + '</td>' +
          '<td>' + t.approvedTeams + '/' + t.maxTeams + '</td><td>' + t.numberOfLegs + ' lượt</td><td>' + stepper(t.status) + '</td>' +
          '<td class="al-right"><div class="cell-actions">' +
          (next ? '<button class="btn btn-sm btn-primary" data-act="next" data-id="' + t.id + '" data-next="' + next + '">Chuyển sang “' + App.TOURNAMENT_STATUS[next] + '”</button>' : '') +
          (t.status !== 'Finished' ? '<button class="btn btn-sm" data-act="edit" data-id="' + t.id + '">Sửa</button>' : '') +
          '<button class="btn btn-sm btn-danger" data-act="delete" data-id="' + t.id + '"' + (t.status === 'Draft' ? '' : ' disabled title="Chỉ xóa được giải ở trạng thái Draft"') + '>Xóa</button>' +
          '</div></td></tr>';
      }).join('') + '</tbody></table></div>';
  }

  async function load() {
    var res = await App.getAll('/api/tournaments');
    items = res.ok ? res.data : [];
    render();
  }

  $('#tournament-table').addEventListener('click', async function (e) {
    var btn = e.target.closest('[data-act]');
    if (!btn || btn.disabled) return;
    var t = items.filter(function (x) { return x.id === +btn.dataset.id; })[0];

    if (btn.dataset.act === 'next') {
      if (await App.confirm('Chuyển “' + t.name + '” sang ' + App.TOURNAMENT_STATUS[btn.dataset.next] + '?')) {
        if (App.report(await App.patch('/api/tournaments/' + t.id + '/status', { status: btn.dataset.next }))) load();
      }
    } else if (btn.dataset.act === 'delete') {
      if (await App.confirm('Xóa giải “' + t.name + '”?')) {
        if (App.report(await App.del('/api/tournaments/' + t.id))) load();
      }
    } else if (btn.dataset.act === 'edit') {
      var v = await App.formDialog({
        title: 'Sửa giải đấu',
        fields: [
          { name: 'name', label: 'Tên giải', value: t.name, required: true },
          { name: 'season', label: 'Mùa giải', value: t.season, required: true },
          { name: 'startDate', label: 'Ngày bắt đầu', type: 'date', value: t.startDate, required: true },
          { name: 'endDate', label: 'Ngày kết thúc', type: 'date', value: t.endDate, required: true },
          { name: 'maxTeams', label: 'Số đội tối đa', type: 'number', min: 2, value: t.maxTeams, required: true },
          { name: 'numberOfLegs', label: 'Số lượt', type: 'select', value: t.numberOfLegs, options: [{ value: 1, label: '1 lượt' }, { value: 2, label: '2 lượt' }] },
        ],
      });
      if (v) {
        v.maxTeams = +v.maxTeams;
        v.numberOfLegs = +v.numberOfLegs;
        if (App.report(await App.put('/api/tournaments/' + t.id, v))) load();
      }
    }
  });

  var form = $('#create-form');
  form.addEventListener('submit', async function (e) {
    e.preventDefault();
    var body = {
      name: form.elements.name.value.trim(),
      season: form.elements.season.value.trim(),
      startDate: form.elements.startDate.value,
      endDate: form.elements.endDate.value,
      maxTeams: +form.elements.maxTeams.value,
      numberOfLegs: +form.elements.numberOfLegs.value,
    };
    if (App.report(await App.post('/api/tournaments', body))) {
      form.reset();
      form.elements.maxTeams.value = 8;
      load();
    }
  });

  load();
};
