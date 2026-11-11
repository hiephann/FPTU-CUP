using fptu_cup.Common;
using fptu_cup.Common.Enums;
using fptu_cup.Entities;
using fptu_cup.Identity;
using fptu_cup.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace fptu_cup.Controllers
{
    /// <summary>
    /// Đăng nhập / đăng ký / đăng xuất của giao diện MVC bằng ASP.NET Core Identity (cookie).
    /// Đăng nhập thành công còn cấp JWT để JS gọi /api/... như cũ.
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true)]
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IConfiguration _config;
        private readonly AccountEmailService _emails;
        private readonly ILogger<AccountController> _logger;

        public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, IConfiguration config,
            AccountEmailService emails, ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
            _emails = emails;
            _logger = logger;
        }

        public static string HomeFor(string? role) => role switch
        {
            nameof(UserRole.Admin) => "/admin",
            nameof(UserRole.TeamManager) => "/manager/team",
            nameof(UserRole.Referee) => "/referee/matches",
            _ => "/",
        };

        [HttpGet("/login")]
        public async Task<IActionResult> Login(string? next)
        {
            // Mở trang đăng nhập = bắt đầu phiên mới: xóa cookie cũ để khớp với JWT trong localStorage (JS cũng xóa).
            // Chuyển hướng lại để form được sinh antiforgery token cho người dùng ẩn danh.
            if (User.Identity?.IsAuthenticated == true)
            {
                await _signInManager.SignOutAsync();
                return RedirectToAction(nameof(Login), new { next });
            }
            return View(new LoginViewModel { Next = next });
        }

        [HttpPost("/login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByNameAsync(model.Username.Trim());
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            {
                ModelState.AddModelError(string.Empty, "Sai tên đăng nhập hoặc mật khẩu");
                return View(model);
            }
            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản đang bị khóa");
                return View(model);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);

            var (token, _) = JwtTokenFactory.Create(_config, user);
            var redirect = !string.IsNullOrEmpty(model.Next) && Url.IsLocalUrl(model.Next) ? model.Next : HomeFor(user.Role.ToString());
            return View("SignedIn", new SignedInViewModel
            {
                Token = token,
                User = new { user.Id, user.FullName, role = user.Role },
                RedirectUrl = redirect,
            });
        }

        [HttpGet("/register")]
        public IActionResult Register() => View(new RegisterViewModel());

        /// <summary>FR01: tạo tài khoản TeamManager qua UserManager (kiểm tra trùng username/email, độ dài mật khẩu).</summary>
        [HttpPost("/register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new User
            {
                Username = model.Username.Trim(),
                Email = model.Email.Trim().ToLowerInvariant(),
                FullName = model.FullName.Trim(),
                Role = UserRole.TeamManager, // Admin và Referee không tự đăng ký được
                EmailConfirmed = false,      // xác nhận qua link gửi về email
            };
            if (await _userManager.FindByNameAsync(user.Username) != null)
                ModelState.AddModelError(nameof(model.Username), "Tên đăng nhập đã tồn tại");
            if (await _userManager.FindByEmailAsync(user.Email) != null)
                ModelState.AddModelError(nameof(model.Email), "Email đã tồn tại");
            if (!ModelState.IsValid) return View(model);

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View(model);
            }

            TempData["Success"] = await TrySend(() => _emails.SendConfirmationAsync(user, HttpContext))
                ? $"Tạo tài khoản thành công. Đã gửi email xác nhận tới {user.Email}. Bạn vẫn đăng nhập được ngay."
                : "Tạo tài khoản thành công nhưng chưa gửi được email xác nhận; có thể gửi lại trong trang Hồ sơ.";
            return RedirectToAction(nameof(Login));
        }

        // ------------------------------------------------------------------ Xác nhận email

        /// <summary>Link trong email xác nhận: /confirm-email?userId=..&token=.. (hiệu lực 24 giờ).</summary>
        [HttpGet("/confirm-email")]
        public async Task<IActionResult> ConfirmEmail(int userId, string? token)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            var ok = user != null && !string.IsNullOrEmpty(token) && (await _userManager.ConfirmEmailAsync(user, token)).Succeeded;
            ViewData["Email"] = user?.Email;
            return View(ok);
        }

        // ------------------------------------------------------------------ Quên / đặt lại mật khẩu

        [HttpGet("/forgot-password")]
        public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

        /// <summary>
        /// Gửi link đặt lại mật khẩu (hiệu lực 30 phút) nếu email thuộc một tài khoản đang hoạt động.
        /// Luôn trả cùng một thông báo để không lộ email nào đã đăng ký.
        /// </summary>
        [HttpPost("/forgot-password")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (user != null && user.IsActive && !await TrySend(() => _emails.SendPasswordResetAsync(user, HttpContext)))
            {
                ModelState.AddModelError(string.Empty, "Không gửi được email lúc này, vui lòng thử lại sau hoặc liên hệ ban tổ chức.");
                return View(model);
            }

            TempData["Sent"] = model.Email.Trim();
            return RedirectToAction(nameof(ForgotPassword));
        }

        [HttpGet("/reset-password")]
        public IActionResult ResetPassword(int userId, string? token)
        {
            if (userId <= 0 || string.IsNullOrEmpty(token)) return RedirectToAction(nameof(ForgotPassword));
            return View(new ResetPasswordViewModel { UserId = userId, Token = token });
        }

        /// <summary>Đặt mật khẩu mới bằng token trong email; thành công thì đổi SecurityStamp (link cũ, phiên cũ hết hiệu lực).</summary>
        [HttpPost("/reset-password")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId.ToString());
            var result = user == null ? null : await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
            if (result == null || !result.Succeeded)
            {
                if (result == null || result.Errors.Any(e => e.Code == "InvalidToken"))
                    ModelState.AddModelError(string.Empty, "Link đặt lại mật khẩu không hợp lệ hoặc đã hết hạn (30 phút). Hãy yêu cầu link mới.");
                else
                    foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View(model);
            }

            // Đặt lại được qua email nghĩa là người dùng sở hữu email đó.
            if (!user!.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
            }
            TempData["Success"] = "Đã đặt lại mật khẩu. Hãy đăng nhập bằng mật khẩu mới.";
            return RedirectToAction(nameof(Login));
        }

        /// <summary>
        /// Cấp lại cookie cho người đang đăng nhập (sau khi sửa hồ sơ / đổi mật khẩu) để tên hiển thị và SecurityStamp trong cookie khớp DB.
        /// Chỉ làm mới phiên của chính mình nên không cần antiforgery.
        /// </summary>
        [HttpPost("/account/refresh-session")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> RefreshSession()
        {
            if (User.Identity?.IsAuthenticated != true) return Unauthorized();
            var user = await _userManager.GetUserAsync(User);
            if (user == null || !user.IsActive) return Unauthorized();
            await _signInManager.RefreshSignInAsync(user);
            return NoContent();
        }

        private async Task<bool> TrySend(Func<Task> send)
        {
            try
            {
                await send();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gửi email thất bại");
                return false;
            }
        }

        // Không bắt antiforgery: cookie có thể đã hết hạn khi bấm đăng xuất, đăng xuất hộ cũng không gây hại.
        [HttpPost("/logout")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        [HttpGet("/access-denied")]
        public IActionResult AccessDenied() => View();
    }
}
