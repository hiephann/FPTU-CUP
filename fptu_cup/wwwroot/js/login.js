/* Đăng nhập: POST /api/auth/login, lưu JWT rồi chuyển tới khu vực theo vai trò. */
(function () {
  var form = App.$('#login-form');
  var error = App.$('#login-error');

  App.$$('[data-fill]').forEach(function (btn) {
    btn.addEventListener('click', function () {
      var parts = btn.getAttribute('data-fill').split('|');
      form.elements.username.value = parts[0];
      form.elements.password.value = parts[1];
    });
  });

  form.addEventListener('submit', async function (e) {
    e.preventDefault();
    error.hidden = true;
    var username = form.elements.username.value.trim();
    var password = form.elements.password.value;
    if (!username || !password) {
      error.textContent = 'Vui lòng nhập tên đăng nhập và mật khẩu.';
      error.hidden = false;
      return;
    }

    var res = await App.post('/api/auth/login', { username: username, password: password });
    if (!res.ok) {
      error.textContent = res.message;
      error.hidden = false;
      return;
    }

    App.setSession(res.data.accessToken, res.data.user);
    var next = new URLSearchParams(location.search).get('next');
    location.href = next && next.charAt(0) === '/' ? next : App.homeFor(res.data.user.role);
  });
})();
