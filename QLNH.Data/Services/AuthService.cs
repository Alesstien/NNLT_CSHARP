using Microsoft.EntityFrameworkCore;
using QLNH.Data.Entities;

namespace QLNH.Data.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    public AuthService(AppDbContext db) => _db = db;

    /// <summary>Trả về tài khoản nếu đúng tên đăng nhập/mật khẩu và đang hoạt động; ngược lại null.</summary>
    public TaiKhoan? DangNhap(string tenDangNhap, string matKhau)
    {
        var tk = _db.TaiKhoans.Include(t => t.VaiTro)
                    .FirstOrDefault(t => t.TenDangNhap == tenDangNhap && t.HoatDong);
        return tk != null && PasswordHasher.Verify(matKhau, tk.MatKhauHash) ? tk : null;
    }
}
