/* Đăng ký Team Manager (FR01): kiểm tra phía client rồi POST /api/auth/register. */
(function () {
  var form = App.$('#register-form');
  var serverError = App.$('#register-error');

  function setError(name, message) {
    var el = App.$('[data-error-for="' + name + '"]', form);
    el.textContent = message || '';
    el.hidden = !message;
    form.elements[name].setAttribute('aria-invalid', message ? 'true' : 'false');
  }

  function validate() {
    var v = function (n) { return (form.elements[n].value || '').trim(); };
    var rules = [
      ['fullName', !v('fullName'), 'Vui lòng nhập họ và tên.'],
      ['email', !/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(v('email')), 'Email không đúng định dạng.'],
      ['username', v('username').length < 3, 'Tên đăng nhập tối thiểu 3 ký tự.'],
      ['password', form.elements.password.value.length < 8, 'Mật khẩu tối thiểu 8 ký tự.'],
      ['confirm', !form.elements.confirm.value || form.elements.confirm.value !== form.elements.password.value, 'Mật khẩu nhập lại không khớp.'],
    ];
    var ok = true;
    rules.forEach(function (r) {
      setError(r[0], r[1] ? r[2] : '');
      if (r[1]) ok = false;
    });
    return ok;
  }

  form.addEventListener('submit', async function (e) {
    e.preventDefault();
    serverError.hidden = true;
    if (!validate()) return;

    var res = await App.post('/api/auth/register', {
      username: form.elements.username.value.trim(),
      password: form.elements.password.value,
      fullName: form.elements.fullName.value.trim(),
      email: form.elements.email.value.trim(),
    });
    if (!res.ok) {
      serverError.textContent = res.message;
      serverError.hidden = false;
      return;
    }
    App.toast('Tạo tài khoản thành công, đang chuyển sang trang đăng nhập…');
    setTimeout(function () { location.href = '/login'; }, 1200);
  });
})();
