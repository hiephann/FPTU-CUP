using System.Net;
using fptu_cup.Common;
using fptu_cup.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace fptu_cup.Identity
{
    /// <summary>Token xác nhận email sống 24 giờ (token đặt lại mật khẩu dùng provider mặc định, 30 phút).</summary>
    public class EmailConfirmationTokenProviderOptions : DataProtectionTokenProviderOptions
    {
        public EmailConfirmationTokenProviderOptions()
        {
            Name = AccountEmailService.ConfirmProvider;
            TokenLifespan = TimeSpan.FromHours(24);
        }
    }

    public class EmailConfirmationTokenProvider : DataProtectorTokenProvider<User>
    {
        public EmailConfirmationTokenProvider(IDataProtectionProvider dataProtectionProvider,
            IOptions<EmailConfirmationTokenProviderOptions> options, ILogger<DataProtectorTokenProvider<User>> logger)
            : base(dataProtectionProvider, options, logger) { }
    }

    /// <summary>Sinh token bằng UserManager (ASP.NET Core Identity) rồi gửi email chứa link xác nhận / đặt lại mật khẩu.</summary>
    public class AccountEmailService
    {
        public const string ConfirmProvider = "EmailConfirmation";
        public static readonly TimeSpan ResetLifespan = TimeSpan.FromMinutes(30);

        private readonly UserManager<User> _users;
        private readonly LinkGenerator _links;
        private readonly IAppEmailSender _sender;

        public AccountEmailService(UserManager<User> users, LinkGenerator links, IAppEmailSender sender)
        {
            _users = users;
            _links = links;
            _sender = sender;
        }

        public async Task SendConfirmationAsync(User user, HttpContext http)
        {
            var token = await _users.GenerateEmailConfirmationTokenAsync(user);
            var url = _links.GetUriByAction(http, "ConfirmEmail", "Account", new { userId = user.Id, token })!;
            await _sender.SendAsync(user.Email, "[FPTU Cup] Xác nhận địa chỉ email", Layout(user.FullName,
                "Bấm nút bên dưới để xác nhận email cho tài khoản <b>" + WebUtility.HtmlEncode(user.Username) + "</b>. Link có hiệu lực trong 24 giờ.",
                url, "Xác nhận email"));
        }

        public async Task SendPasswordResetAsync(User user, HttpContext http)
        {
            var token = await _users.GeneratePasswordResetTokenAsync(user);
            var url = _links.GetUriByAction(http, "ResetPassword", "Account", new { userId = user.Id, token })!;
            await _sender.SendAsync(user.Email, "[FPTU Cup] Đặt lại mật khẩu", Layout(user.FullName,
                "Có yêu cầu đặt lại mật khẩu cho tài khoản <b>" + WebUtility.HtmlEncode(user.Username) + "</b>. " +
                "Link có hiệu lực trong 30 phút và chỉ dùng được một lần. Nếu không phải bạn yêu cầu, hãy bỏ qua email này.",
                url, "Đặt lại mật khẩu"));
        }

        private static string Layout(string name, string text, string url, string button)
        {
            var safeUrl = WebUtility.HtmlEncode(url);
            return $"""
                <div style="font-family:Arial,sans-serif;max-width:520px;margin:auto;color:#10201A">
                  <div style="background:#10201A;color:#fff;padding:16px 20px;border-radius:12px 12px 0 0;font-weight:700;font-size:18px">FPTU CUP 2026</div>
                  <div style="border:1px solid #E3E7DF;border-top:0;padding:20px;border-radius:0 0 12px 12px">
                    <p>Xin chào {WebUtility.HtmlEncode(name)},</p>
                    <p>{text}</p>
                    <p style="margin:24px 0"><a href="{safeUrl}" style="background:#1F7A4D;color:#fff;padding:12px 20px;border-radius:8px;text-decoration:none;font-weight:700">{button}</a></p>
                    <p style="font-size:13px;color:#4A5A52">Nếu nút không bấm được, mở link sau:<br><a href="{safeUrl}">{safeUrl}</a></p>
                  </div>
                </div>
                """;
        }
    }
}
