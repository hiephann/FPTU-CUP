using System.ComponentModel.DataAnnotations;

namespace fptu_cup.Models
{
    // ViewModel cho form MVC: Data Annotations sinh kiểm tra phía client (jQuery Validation Unobtrusive)
    // và được kiểm tra lại phía server bằng ModelState.

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = "";

        public string? Next { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [StringLength(100, ErrorMessage = "Họ và tên tối đa 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự")]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3 đến 50 ký tự")]
        [RegularExpression(@"^[A-Za-z0-9._@+-]+$", ErrorMessage = "Tên đăng nhập chỉ gồm chữ không dấu, số và các ký tự . _ @ + -")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu")]
        [Compare(nameof(Password), ErrorMessage = "Mật khẩu nhập lại không khớp")]
        [DataType(DataType.Password)]
        [Display(Name = "Nhập lại mật khẩu")]
        public string ConfirmPassword { get; set; } = "";
    }

    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [Display(Name = "Email đã đăng ký")]
        public string Email { get; set; } = "";
    }

    public class ResetPasswordViewModel
    {
        [Required] public int UserId { get; set; }
        [Required] public string Token { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu")]
        [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu nhập lại không khớp")]
        [DataType(DataType.Password)]
        [Display(Name = "Nhập lại mật khẩu mới")]
        public string ConfirmPassword { get; set; } = "";
    }

    /// <summary>Sau khi đăng nhập cookie thành công: giao JWT cho JS (localStorage) để gọi /api rồi chuyển trang.</summary>
    public class SignedInViewModel
    {
        public string Token { get; set; } = "";
        public object User { get; set; } = new();
        public string RedirectUrl { get; set; } = "/";
    }
}
