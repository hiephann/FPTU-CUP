/*
 * Đăng nhập: form POST /login (ASP.NET Core Identity, kiểm tra bằng Data Annotations + jQuery Validation).
 * Script này chỉ điền nhanh tài khoản mẫu và xóa JWT cũ (vào trang đăng nhập = bắt đầu phiên mới).
 */
(function () {
  App.clearSession();
  var form = App.$('#login-form');
  App.$$('[data-fill]').forEach(function (btn) {
    btn.addEventListener('click', function () {
      var parts = btn.getAttribute('data-fill').split('|');
      form.elements.Username.value = parts[0];
      form.elements.Password.value = parts[1];
    });
  });
})();
