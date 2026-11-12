using Microsoft.EntityFrameworkCore;
using QLNH.Data.Entities;

namespace QLNH.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<VaiTro> VaiTros => Set<VaiTro>();
    public DbSet<TaiKhoan> TaiKhoans => Set<TaiKhoan>();
    public DbSet<DanhMuc> DanhMucs => Set<DanhMuc>();
    public DbSet<MonAn> MonAns => Set<MonAn>();
    public DbSet<Ban> Bans => Set<Ban>();
    public DbSet<DonHang> DonHangs => Set<DonHang>();
    public DbSet<ChiTietDonHang> ChiTietDonHangs => Set<ChiTietDonHang>();
    public DbSet<DatBan> DatBans => Set<DatBan>();
    public DbSet<WebUuDai> WebUuDais => Set<WebUuDai>();

    public static AppDbContext Create(string connectionString)
    {
        var opt = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new AppDbContext(opt);
    }

    public void EnsureSchemaColumns()
    {
        if (!Database.IsSqlServer()) return;
        Database.ExecuteSqlRaw("""
            IF OBJECT_ID(N'dbo.DonHang', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH(N'dbo.DonHang', N'MaDatBan') IS NULL
                    ALTER TABLE dbo.DonHang ADD MaDatBan NVARCHAR(30) NULL;
                IF COL_LENGTH(N'dbo.DonHang', N'MaUuDai') IS NULL
                    ALTER TABLE dbo.DonHang ADD MaUuDai NVARCHAR(30) NULL;
                IF COL_LENGTH(N'dbo.DonHang', N'GiamGia') IS NULL
                    ALTER TABLE dbo.DonHang ADD GiamGia DECIMAL(18, 0) NOT NULL CONSTRAINT DF_DonHang_GiamGia DEFAULT (0);
            END;

            IF OBJECT_ID(N'dbo.DatBan', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH(N'dbo.DatBan', N'MaDatBan') IS NULL
                    ALTER TABLE dbo.DatBan ADD MaDatBan NVARCHAR(30) NULL;
                IF COL_LENGTH(N'dbo.DatBan', N'BatDauPhucVuLuc') IS NULL
                    ALTER TABLE dbo.DatBan ADD BatDauPhucVuLuc DATETIME2 NULL;
            END;
            """);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<VaiTro>().ToTable("VaiTro");

        b.Entity<TaiKhoan>(e =>
        {
            e.ToTable("TaiKhoan");
            e.HasIndex(x => x.TenDangNhap).IsUnique();
        });

        b.Entity<DanhMuc>().ToTable("DanhMuc");

        b.Entity<MonAn>(e =>
        {
            e.ToTable("MonAn");
            e.Property(x => x.Gia).HasColumnType("decimal(18,0)");
            e.HasOne(x => x.DanhMuc).WithMany(d => d.MonAns).HasForeignKey(x => x.DanhMucId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Ban>().ToTable("Ban");

        b.Entity<DonHang>(e =>
        {
            e.ToTable("DonHang");
            e.Property(x => x.GiamGia).HasPrecision(18, 0);
            e.Property(x => x.TongTien).HasColumnType("decimal(18,0)");
            e.HasOne(x => x.Ban).WithMany().HasForeignKey(x => x.BanId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<ChiTietDonHang>(e =>
        {
            e.ToTable("ChiTietDonHang");
            e.Property(x => x.DonGia).HasColumnType("decimal(18,0)");
            e.Ignore(x => x.ThanhTien);
            e.HasOne(x => x.DonHang).WithMany(d => d.ChiTiets).HasForeignKey(x => x.DonHangId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.MonAn).WithMany().HasForeignKey(x => x.MonAnId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<DatBan>(e =>
        {
            e.ToTable("DatBan");
            e.HasOne(x => x.Ban).WithMany().HasForeignKey(x => x.BanId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<WebUuDai>(e =>
        {
            e.ToTable("Web_UuDai", t => t.ExcludeFromMigrations());
            e.HasIndex(x => x.Ma).IsUnique();
            e.Property(x => x.Ma).HasMaxLength(30);
            e.Property(x => x.MoTa).HasMaxLength(200);
            e.Property(x => x.GiamToiDa).HasPrecision(18, 0);
        });
    }
}
