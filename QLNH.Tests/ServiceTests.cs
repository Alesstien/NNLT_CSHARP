using Microsoft.EntityFrameworkCore;
using QLNH.Data;
using QLNH.Data.Entities;
using QLNH.Data.Services;
using Xunit;

namespace QLNH.Tests;

public class ServiceTests
{
    private static AppDbContext NewDb()
    {
        var opt = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(opt);
        db.DanhMucs.Add(new DanhMuc { Id = 1, TenDanhMuc = "Món chính" });
        db.MonAns.AddRange(new MonAn { Id = 1, TenMon = "Cơm tấm", Gia = 65000, DanhMucId = 1 },
                           new MonAn { Id = 2, TenMon = "Trà đào", Gia = 39000, DanhMucId = 1 },
                           new MonAn { Id = 3, TenMon = "Món ngừng bán", Gia = 10000, DanhMucId = 1, ConPhucVu = false });
        db.Bans.Add(new Ban { Id = 1, TenBan = "Bàn 01", SoGhe = 4 });
        db.SaveChanges();
        return db;
    }

    [Fact]
    public void PasswordHasher_DungSaiMatKhau()
    {
        var h = PasswordHasher.Hash("admin123");
        Assert.True(PasswordHasher.Verify("admin123", h));
        Assert.False(PasswordHasher.Verify("sai", h));
        Assert.False(PasswordHasher.Verify("x", "khong-hop-le"));
    }

    [Fact]
    public void TaoDon_TinhTongTheoGiaTrongCsdl_VaGomMonTrung()
    {
        using var db = NewDb();
        var don = new OrderService(db).TaoDon("An", "0901234567", 1, null, new[] { new DongDat(1, 2), new DongDat(2, 1), new DongDat(1, 1) });
        Assert.Equal(3 * 65000 + 39000, don.TongTien);
        Assert.Equal(2, don.ChiTiets.Count);
        Assert.Equal("Đang dùng", db.Bans.Find(1)!.TrangThai);
    }

    [Fact]
    public void TaoDon_DonRong_NemLoi()
    {
        using var db = NewDb();
        Assert.Throws<ArgumentException>(() => new OrderService(db).TaoDon("An", null, null, null, Array.Empty<DongDat>()));
    }

    [Fact]
    public void TaoDon_MonNgungPhucVu_NemLoi()
    {
        using var db = NewDb();
        Assert.Throws<InvalidOperationException>(() => new OrderService(db).TaoDon("An", null, null, null, new[] { new DongDat(3, 1) }));
    }

    [Fact]
    public void ThanhToan_GiaiPhongBan_VaTinhDoanhThu()
    {
        using var db = NewDb();
        var svc = new OrderService(db);
        var don = svc.TaoDon("An", null, 1, null, new[] { new DongDat(1, 2) });
        svc.DoiTrangThai(don.Id, TrangThaiDon.DaThanhToan);
        Assert.Equal("Trống", db.Bans.Find(1)!.TrangThai);
        var dt = svc.DoanhThuTheoNgay(DateTime.Today, DateTime.Today);
        Assert.Single(dt);
        Assert.Equal(130000, dt[0].DoanhThu);
    }
}
