using System.Net;
using System.Net.Mail;
using System.Text;

namespace fptu_cup.Common
{
    /// <summary>
    /// Cấu hình mục "Email" trong appsettings / user-secrets.
    /// Mật khẩu SMTP (vd App Password của Gmail) KHÔNG để trong appsettings.json: đặt bằng
    /// <c>dotnet user-secrets set "Email:Password" "..."</c>.
    /// </summary>
    public class EmailOptions
    {
        public string Host { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string? Username { get; set; }
        public string? Password { get; set; }
        /// <summary>Địa chỉ người gửi; bỏ trống thì dùng Username (Gmail bắt buộc trùng tài khoản đăng nhập).</summary>
        public string? From { get; set; }
        public string FromName { get; set; } = "FPTU Cup";
        /// <summary>Chưa cấu hình Username/Password thì email được ghi thành file .eml ở thư mục này (để chạy thử).</summary>
        public string PickupDirectory { get; set; } = "App_Data/mails";

        public bool SmtpConfigured => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
    }

    public interface IAppEmailSender
    {
        Task SendAsync(string to, string subject, string htmlBody);
    }

    /// <summary>Gửi email qua SMTP (System.Net.Mail); chưa cấu hình SMTP thì lưu file .eml vào PickupDirectory.</summary>
    public class SmtpEmailSender : IAppEmailSender
    {
        private readonly EmailOptions _options;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(Microsoft.Extensions.Options.IOptions<EmailOptions> options, IWebHostEnvironment env, ILogger<SmtpEmailSender> logger)
        {
            _options = options.Value;
            _env = env;
            _logger = logger;
        }

        public async Task SendAsync(string to, string subject, string htmlBody)
        {
            var from = !string.IsNullOrWhiteSpace(_options.From) ? _options.From
                : !string.IsNullOrWhiteSpace(_options.Username) ? _options.Username
                : "no-reply@fptucup.local";
            using var message = new MailMessage
            {
                From = new MailAddress(from, _options.FromName, Encoding.UTF8),
                Subject = subject,
                SubjectEncoding = Encoding.UTF8,
                Body = htmlBody,
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = true,
            };
            message.To.Add(to);

            using var client = new SmtpClient();
            if (_options.SmtpConfigured)
            {
                client.Host = _options.Host;
                client.Port = _options.Port;
                client.EnableSsl = _options.EnableSsl;
                client.Credentials = new NetworkCredential(_options.Username, _options.Password);
            }
            else
            {
                var dir = Path.Combine(_env.ContentRootPath, _options.PickupDirectory);
                Directory.CreateDirectory(dir);
                client.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
                client.PickupDirectoryLocation = dir;
                _logger.LogWarning("Chưa cấu hình SMTP (Email:Username/Email:Password) – email \"{Subject}\" được lưu ở {Dir}", subject, dir);
            }

            await client.SendMailAsync(message);
        }
    }
}
