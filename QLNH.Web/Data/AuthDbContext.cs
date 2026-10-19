using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QLNH_Web.Models;

namespace QLNH_Web.Data;

// Context riêng của Web (tài khoản khách + đặt bàn), cùng database QLNhaHang
// nhưng KHÔNG đụng tới AppDbContext của QLNH.Data / WinForms.
// Bảng Identity dùng tên mặc định (AspNet*), bảng nghiệp vụ có tiền tố Web_ để không trùng bảng sẵn có.
public class AuthDbContext : IdentityDbContext<AppUser>
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    public DbSet<KhuVuc> KhuVucs => Set<KhuVuc>();
    public DbSet<MucThucDon> MucThucDons => Set<MucThucDon>();
    public DbSet<UuDai> UuDais => Set<UuDai>();
    public DbSet<DatBan> DatBans => Set<DatBan>();
    public DbSet<DatBanMon> DatBanMons => Set<DatBanMon>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<AppUser>().Property(u => u.HoTen).HasMaxLength(100);

        b.Entity<KhuVuc>(e =>
        {
            e.ToTable("Web_KhuVuc");
            e.Property(x => x.Ten).HasMaxLength(100);
            e.Property(x => x.MoTa).HasMaxLength(300);
            e.Property(x => x.PhuThu).HasPrecision(18, 0);
        });

        b.Entity<MucThucDon>(e =>
        {
            e.ToTable("Web_MucThucDon");
            e.Property(x => x.Ten).HasMaxLength(150);
            e.Property(x => x.MoTa).HasMaxLength(400);
            e.Property(x => x.DanhMuc).HasMaxLength(60);
            e.Property(x => x.HinhAnh).HasMaxLength(500);
            e.Property(x => x.Emoji).HasMaxLength(16);
            e.Property(x => x.Gia).HasPrecision(18, 0);
        });

        b.Entity<UuDai>(e =>
        {
            e.ToTable("Web_UuDai");
            e.HasIndex(x => x.Ma).IsUnique();
            e.Property(x => x.Ma).HasMaxLength(30);
            e.Property(x => x.MoTa).HasMaxLength(200);
            e.Property(x => x.GiamToiDa).HasPrecision(18, 0);
        });

        b.Entity<DatBan>(e =>
        {
            e.ToTable("Web_DatBan");
            e.HasIndex(x => x.MaDatBan).IsUnique();
            e.HasIndex(x => new { x.KhuVucId, x.NgayGio });
            e.Property(x => x.MaDatBan).HasMaxLength(30);
            e.Property(x => x.HoTen).HasMaxLength(100);
            e.Property(x => x.SoDienThoai).HasMaxLength(20);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.DipDacBiet).HasMaxLength(100);
            e.Property(x => x.GhiChu).HasMaxLength(500);
            e.Property(x => x.PhuongThucThanhToan).HasMaxLength(30);
            e.Property(x => x.MaGiaoDich).HasMaxLength(40);
            e.Property(x => x.TrangThai).HasMaxLength(30);
            e.Property(x => x.LyDoHuy).HasMaxLength(300);
            e.Property(x => x.MaUuDai).HasMaxLength(30);
            e.Property(x => x.CheDoCoc).HasMaxLength(20);
            e.Property(x => x.NganHang).HasMaxLength(10);
            e.Property(x => x.PhuThuKhuVuc).HasPrecision(18, 0);
            e.Property(x => x.TongMon).HasPrecision(18, 0);
            e.Property(x => x.Vat).HasPrecision(18, 0);
            e.Property(x => x.PhiPhucVu).HasPrecision(18, 0);
            e.Property(x => x.GiamGia).HasPrecision(18, 0);
            e.Property(x => x.TongHoaDon).HasPrecision(18, 0);
            e.Property(x => x.TienCoc).HasPrecision(18, 0);
            e.Property(x => x.ConLai).HasPrecision(18, 0);
            e.HasOne(x => x.KhuVuc).WithMany().HasForeignKey(x => x.KhuVucId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<DatBanMon>(e =>
        {
            e.ToTable("Web_DatBanMon");
            e.Property(x => x.TenMon).HasMaxLength(150);
            e.Property(x => x.DonGia).HasPrecision(18, 0);
            e.HasOne(x => x.DatBan).WithMany(d => d.Mons).HasForeignKey(x => x.DatBanId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Mon).WithMany().HasForeignKey(x => x.MucThucDonId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
