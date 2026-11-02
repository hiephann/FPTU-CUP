/* Hồ sơ cá nhân: GET /api/auth/me và PUT /api/auth/change-password (FR03). */
App.main = async function () {
  if (!App.token()) { location.href = '/login?next=/account'; return; }
  var ROLE = { Admin: 'Admin (ban tổ chức)', Referee: 'Trọng tài', TeamManager: 'Quản lý đội' };

  var res = await App.get('/api/auth/me');
  if (!res.ok) { App.$('#profile').innerHTML = '<p class="muted">' + App.esc(res.message) + '</p>'; return; }
  var u = res.data;
  App.$('#profile').innerHTML =
    '<h2 class="card-title">' + App.esc(u.fullName) + '</h2><div class="chips"><span class="pill pill-chip">' + ROLE[u.role] + '</span></div>' +
    '<p class="muted">Tên đăng nhập: <b>' + App.esc(u.username) + '</b> · Email: <b>' + App.esc(u.email) + '</b></p>' +
    '<div><a class="btn btn-sm" href="' + App.homeFor(u.role) + '">Về khu vực làm việc</a> <button class="btn btn-sm" id="logout" type="button">Đăng xuất</button></div>';
  App.$('#logout').addEventListener('click', App.logout);

  var form = App.$('#password-form');
  var error = App.$('#password-error');
  form.addEventListener('submit', async function (e) {
    e.preventDefault();
    error.hidden = true;
    var f = form.elements;
    var problem = !f.oldPassword.value ? 'Vui lòng nhập mật khẩu hiện tại.'
      : f.newPassword.value.length < 8 ? 'Mật khẩu mới tối thiểu 8 ký tự.'
      : f.newPassword.value !== f.confirm.value ? 'Mật khẩu nhập lại không khớp.' : '';
    if (problem) { error.textContent = problem; error.hidden = false; return; }

    var r = await App.put('/api/auth/change-password', { oldPassword: f.oldPassword.value, newPassword: f.newPassword.value });
    if (r.ok) { form.reset(); App.toast('Đã đổi mật khẩu'); }
    else { error.textContent = r.message; error.hidden = false; }
  });
};
