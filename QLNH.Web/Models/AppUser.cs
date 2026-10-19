using Microsoft.AspNetCore.Identity;

namespace QLNH_Web.Models;

public class AppUser : IdentityUser
{
    public string HoTen { get; set; } = "";
    public DateTime NgayTao { get; set; } = DateTime.Now;
    public DateTime? LanDangNhapCuoi { get; set; }   // admin xem được ở trang quản lý tài khoản
    public int DiemTichLuy { get; set; }             // dùng cho chương trình thành viên (1.5)
}
