/*
 * Hồ sơ cá nhân (FR03): GET/PUT /api/auth/me, POST /api/auth/resend-confirmation, PUT /api/auth/change-password.
 * Sau khi sửa hồ sơ / đổi mật khẩu gọi POST /account/refresh-session để cookie Identity khớp tên và SecurityStamp mới.
 */
App.main = async function () {
  if (!App.token()) { location.href = '/login?next=/account'; return; }
  var $ = App.$;
  var ROLE = { Admin: 'Admin (ban tổ chức)', Referee: 'Trọng tài', TeamManager: 'Quản lý đội' };
  var u;

  function refreshSession() {
    return fetch('/account/refresh-session', { method: 'POST' }).catch(function () { return null; });
  }

  function renderProfile() {
    var badge = u.emailConfirmed
      ? App.render.statusBadge('ok', 'Email đã xác nhận')
      : App.render.statusBadge('warn', 'Email chưa xác nhận');
    $('#profile').innerHTML =
      '<h2 class="card-title">' + App.esc(u.fullName) + '</h2><div class="chips"><span class="pill pill-chip">' + ROLE[u.role] + '</span>' + badge + '</div>' +
      '<p class="muted">Tên đăng nhập: <b>' + App.esc(u.username) + '</b> · Email: <b>' + App.esc(u.email) + '</b></p>' +
      (u.emailConfirmed ? '' :
        '<div class="banner banner-warn">Email chưa được xác nhận. Xác nhận email để có thể tự lấy lại mật khẩu khi quên. ' +
        '<button class="btn btn-sm" id="resend" type="button">Gửi lại email xác nhận</button></div>') +
      '<div><a class="btn btn-sm" href="' + App.homeFor(u.role) + '">Về khu vực làm việc</a> <button class="btn btn-sm" id="logout" type="button">Đăng xuất</button></div>';
    $('#logout').addEventListener('click', App.logout);
    var resend = $('#resend');
    if (resend) resend.addEventListener('click', async function () {
      resend.disabled = true;
      App.report(await App.post('/api/auth/resend-confirmation'));
      setTimeout(function () { resend.disabled = false; }, 30000); // tránh bấm liên tục
    });

    var f = $('#profile-form').elements;
    f.fullName.value = u.fullName;
    f.email.value = u.email;
    $('#p-pass-field').hidden = true;
  }

  var res = await App.get('/api/auth/me');
  if (!res.ok) { $('#profile').innerHTML = '<p class="muted">' + App.esc(res.message) + '</p>'; return; }
  u = res.data;
  renderProfile();

  // ---------------------------------------------------------------- Sửa hồ sơ
  var pform = $('#profile-form');
  var perror = $('#profile-error');
  pform.elements.email.addEventListener('input', function () {
    $('#p-pass-field').hidden = pform.elements.email.value.trim().toLowerCase() === u.email.toLowerCase();
  });
  pform.addEventListener('submit', async function (e) {
    e.preventDefault();
    perror.hidden = true;
    var f = pform.elements;
    var name = f.fullName.value.trim(), email = f.email.value.trim();
    var emailChanged = email.toLowerCase() !== u.email.toLowerCase();
    var problem = !name ? 'Vui lòng nhập họ và tên.'
      : !/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(email) ? 'Email không đúng định dạng.'
      : emailChanged && !f.currentPassword.value ? 'Đổi email cần nhập mật khẩu hiện tại.' : '';
    if (problem) { perror.textContent = problem; perror.hidden = false; return; }

    var r = await App.put('/api/auth/me', { fullName: name, email: email, currentPassword: f.currentPassword.value || null });
    if (!r.ok) { perror.textContent = r.message; perror.hidden = false; return; }
    App.toast(r.message);
    u = Object.assign(u, r.data);
    var stored = App.user();
    if (stored) App.setSession(App.token(), Object.assign(stored, { fullName: u.fullName }));
    f.currentPassword.value = '';
    renderProfile();
    await refreshSession(); // cập nhật tên hiển thị trên header (cookie)
  });

  // ---------------------------------------------------------------- Đổi mật khẩu
  var form = $('#password-form');
  var error = $('#password-error');
  form.addEventListener('submit', async function (e) {
    e.preventDefault();
    error.hidden = true;
    var f = form.elements;
    var problem = !f.oldPassword.value ? 'Vui lòng nhập mật khẩu hiện tại.'
      : f.newPassword.value.length < 8 ? 'Mật khẩu mới tối thiểu 8 ký tự.'
      : f.newPassword.value !== f.confirm.value ? 'Mật khẩu nhập lại không khớp.' : '';
    if (problem) { error.textContent = problem; error.hidden = false; return; }

    var r = await App.put('/api/auth/change-password', { oldPassword: f.oldPassword.value, newPassword: f.newPassword.value });
    if (r.ok) { form.reset(); App.toast('Đã đổi mật khẩu'); await refreshSession(); }
    else { error.textContent = r.message; error.hidden = false; }
  });
};
