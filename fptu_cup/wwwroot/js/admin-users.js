/* Quản lý tài khoản (FR04): lọc/phân trang /api/users, tạo trọng tài, khóa/mở khóa. */
App.main = function () {
  var $ = App.$;
  var state = { page: 1, search: '', role: '' };
  var ROLE = { Admin: 'Admin', Referee: 'Trọng tài', TeamManager: 'Quản lý đội' };
  var me = App.user();

  async function load() {
    var res = await App.get('/api/users' + App.qs({ page: state.page, pageSize: 10, search: state.search, role: state.role }));
    if (!res.ok) { $('#user-table').innerHTML = '<p class="muted">' + App.esc(res.message) + '</p>'; return; }
    var d = res.data;
    $('#user-count').textContent = d.totalCount + ' tài khoản';

    $('#user-table').innerHTML = d.items.length
      ? '<div class="table-scroll"><table class="tbl tbl-left"><thead><tr><th>Tên đăng nhập</th><th>Họ và tên</th><th>Email</th><th>Vai trò</th><th>Trạng thái</th><th><span class="sr-only">Thao tác</span></th></tr></thead><tbody>' +
        d.items.map(function (u) {
          var self = u.id === me.id;
          return '<tr><td style="font-weight:600">' + App.esc(u.username) + '</td><td>' + App.esc(u.fullName) + '</td><td>' + App.esc(u.email) + '</td>' +
            '<td>' + ROLE[u.role] + '</td><td>' + App.render.statusBadge(u.isActive ? 'ok' : 'error', u.isActive ? 'Hoạt động' : 'Đã khóa') + '</td>' +
            '<td class="al-right"><div class="cell-actions"><button class="btn btn-sm" data-reset="' + u.id + '" data-name="' + App.esc(u.username) + '">Đặt lại mật khẩu</button>' +
            '<button class="btn btn-sm ' + (u.isActive ? 'btn-danger' : '') + '" data-id="' + u.id + '" data-active="' + (u.isActive ? '0' : '1') + '"' +
            (self ? ' disabled title="Không thể tự khóa tài khoản của mình"' : '') + '>' + (u.isActive ? 'Khóa' : 'Mở khóa') + '</button></div></td></tr>';
        }).join('') + '</tbody></table></div>'
      : '<p class="muted">Không có tài khoản nào khớp.</p>';

    $('#user-pager').innerHTML = App.render.pager(d);
  }

  $('#user-filter').addEventListener('submit', function (e) {
    e.preventDefault();
    state.search = e.target.elements.search.value.trim();
    state.role = e.target.elements.role.value;
    state.page = 1;
    load();
  });

  $('#user-pager').addEventListener('click', function (e) {
    var b = e.target.closest('[data-page]');
    if (b && !b.disabled) { state.page = +b.dataset.page; load(); }
  });

  $('#user-table').addEventListener('click', async function (e) {
    var r = e.target.closest('[data-reset]');
    if (r) {
      var v = await App.formDialog({
        title: 'Đặt lại mật khẩu cho ' + r.dataset.name,
        fields: [{ name: 'newPassword', label: 'Mật khẩu mới (tối thiểu 8 ký tự)', type: 'password', required: true, min: 8 }],
        submitText: 'Đặt lại',
      });
      if (v) App.report(await App.put('/api/users/' + r.dataset.reset + '/password', { newPassword: v.newPassword }));
      return;
    }
    var b = e.target.closest('[data-id]');
    if (!b || b.disabled) return;
    var active = b.dataset.active === '1';
    if (await App.confirm((active ? 'Mở khóa' : 'Khóa') + ' tài khoản này?')) {
      if (App.report(await App.patch('/api/users/' + b.dataset.id + '/status', { isActive: active }))) load();
    }
  });

  var form = $('#referee-form');
  form.addEventListener('submit', async function (e) {
    e.preventDefault();
    var f = form.elements;
    var body = { role: f.role.value, fullName: f.fullName.value.trim(), username: f.username.value.trim(), email: f.email.value.trim(), password: f.password.value };
    if (App.report(await App.post('/api/users', body))) { form.reset(); load(); }
  });

  load();
};
