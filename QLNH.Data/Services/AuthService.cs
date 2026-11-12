using Microsoft.EntityFrameworkCore;
using QLNH.Data.Entities;

namespace QLNH.Data.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    public AuthService(AppDbContext db) => _db = db;

    public TaiKhoan? DangNhap(string tenDangNhap, string matKhau)
    {
        var tk = _db.TaiKhoans
            .Include(t => t.VaiTro)
            .FirstOrDefault(t => t.TenDangNhap == tenDangNhap && t.HoatDong);

        var vaiTro = tk?.VaiTro?.TenVaiTro;
        if (tk == null || (vaiTro != "Admin" && vaiTro != "NhanVien"))
        {
            return null;
        }

        return PasswordHasher.Verify(matKhau, tk.MatKhauHash) ? tk : null;
    }
}
