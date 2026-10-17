using System.ComponentModel.DataAnnotations;

namespace QLNH_Web.ViewModels;

public class RegisterVM
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên"), StringLength(100)]
    [Display(Name = "Họ và tên")]
    public string HoTen { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập email"), EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [RegularExpression(@"^(0|\+84)\d{9}$", ErrorMessage = "Số điện thoại không hợp lệ (VD: 0912345678)")]
    [Display(Name = "Số điện thoại")]
    public string SoDienThoai { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu"), DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    [Display(Name = "Mật khẩu")]
    public string MatKhau { get; set; } = "";

    [DataType(DataType.Password), Compare(nameof(MatKhau), ErrorMessage = "Mật khẩu nhập lại không khớp")]
    [Display(Name = "Nhập lại mật khẩu")]
    public string NhapLaiMatKhau { get; set; } = "";
}

public class LoginVM
{
    [Required(ErrorMessage = "Vui lòng nhập email hoặc số điện thoại")]
    [Display(Name = "Email hoặc số điện thoại")]
    public string TaiKhoan { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu"), DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string MatKhau { get; set; } = "";

    [Display(Name = "Ghi nhớ đăng nhập")]
    public bool NhoDangNhap { get; set; }

    public string? ReturnUrl { get; set; }
}
