using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QLNH_Web.Models;

namespace QLNH_Web.Data;

// Context riêng cho tài khoản khách hàng, cùng database QLNhaHang
// nhưng KHÔNG đụng tới AppDbContext của QLNH.Data / WinForms.
// Dùng tên bảng mặc định của Identity (AspNetUsers, AspNetRoles...) để không trùng bảng sẵn có.
public class AuthDbContext : IdentityDbContext<AppUser>
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<AppUser>().Property(u => u.HoTen).HasMaxLength(100);
    }
}
