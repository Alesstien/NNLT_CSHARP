using Microsoft.EntityFrameworkCore;
using QLNH.Data.Entities;

namespace QLNH.Data.Services;

public record DongDat(int MonAnId, int SoLuong);
public record DoanhThuNgay(DateTime Ngay, int SoDon, decimal DoanhThu);
public record MonBanChay(string TenMon, int SoLuong, decimal DoanhThu);

public class OrderService
{
    private readonly AppDbContext _db;
    public OrderService(AppDbContext db) => _db = db;

    /// <summary>Tạo đơn hàng; đơn giá lấy từ CSDL (không tin dữ liệu từ client).</summary>
    public DonHang TaoDon(string tenKhach, string? sdt, int? banId, string? ghiChu, IEnumerable<DongDat> dong)
    {
        if (string.IsNullOrWhiteSpace(tenKhach)) throw new ArgumentException("Tên khách không được để trống.");
        var list = dong.Where(d => d.SoLuong > 0).GroupBy(d => d.MonAnId)
                       .Select(g => new DongDat(g.Key, g.Sum(x => x.SoLuong))).ToList();
        if (list.Count == 0) throw new ArgumentException("Đơn hàng phải có ít nhất một món.");

        var ids = list.Select(x => x.MonAnId).ToList();
        var mons = _db.MonAns.Where(m => ids.Contains(m.Id) && m.ConPhucVu).ToDictionary(m => m.Id);
        if (mons.Count != ids.Count) throw new InvalidOperationException("Có món không tồn tại hoặc đã ngừng phục vụ.");

        var don = new DonHang { TenKhach = tenKhach.Trim(), SoDienThoai = sdt, BanId = banId, GhiChu = ghiChu, NgayTao = DateTime.Now };
        foreach (var d in list)
            don.ChiTiets.Add(new ChiTietDonHang { MonAnId = d.MonAnId, SoLuong = d.SoLuong, DonGia = mons[d.MonAnId].Gia });
        don.TongTien = don.ChiTiets.Sum(c => c.ThanhTien);

        if (banId != null)
        {
            var ban = _db.Bans.Find(banId) ?? throw new InvalidOperationException("Bàn không tồn tại.");
            ban.TrangThai = "Đang dùng";
        }
        _db.DonHangs.Add(don);
        _db.SaveChanges();
        return don;
    }

   public void DoiTrangThai(int donId, string trangThai)
{
    if (!TrangThaiDon.TatCa.Contains(trangThai))
        throw new ArgumentException("Trạng thái không hợp lệ.");

    using var tx = _db.Database.IsSqlServer()
        && _db.Database.CurrentTransaction is null
        ? _db.Database.BeginTransaction()
        : null;

    var don = _db.DonHangs.Find(donId)
        ?? throw new InvalidOperationException("Không tìm thấy đơn hàng.");

    don.TrangThai = trangThai;

    if (don.BanId is int banId && _db.Bans.Find(banId) is Ban ban)
    {
        if (trangThai is TrangThaiDon.DaThanhToan
            or TrangThaiDon.HoanThanh
            or TrangThaiDon.DaHuy)
        {
            ban.TrangThai = "Trống";
        }
        else if (trangThai == TrangThaiDon.DangCheBien)
        {
            ban.TrangThai = "Đang dùng";
        }
        else if (trangThai == TrangThaiDon.DaXacNhan && ban.TrangThai == "Trống")
        {
            ban.TrangThai = "Đã đặt";
        }
    }

    if (don.MaDatBan is string ma && !string.IsNullOrWhiteSpace(ma))
    {
        var trangThaiDatBan = trangThai switch
        {
            TrangThaiDon.DaXacNhan or TrangThaiDon.DangCheBien => "Đã xác nhận",
            TrangThaiDon.DaThanhToan => "Đã đến",
            TrangThaiDon.HoanThanh => null,
            TrangThaiDon.DaHuy => "Đã hủy",
            _ => "Chờ xác nhận"
        };

        var phieu = _db.DatBans.FirstOrDefault(d => d.MaDatBan == ma);

        if (phieu != null && trangThaiDatBan != null)
            phieu.TrangThai = trangThaiDatBan;

        var trangThaiWeb = trangThai switch
        {
            TrangThaiDon.DaXacNhan => "Đã xác nhận",
            TrangThaiDon.DangCheBien => "Đang chế biến",
            TrangThaiDon.HoanThanh => "Đã hoàn thành",
            TrangThaiDon.DaThanhToan => "Đã thanh toán",
            TrangThaiDon.DaHuy => "Đã hủy",
            _ => "Đang chờ duyệt"
        };

        if (_db.Database.IsSqlServer())
        {
            _db.Database.ExecuteSqlInterpolated($"""
                IF OBJECT_ID(N'dbo.Web_DatBan', N'U') IS NOT NULL
                BEGIN
                    UPDATE dbo.Web_DatBan
                    SET TrangThai = {trangThaiWeb},
                        NgayCapNhat = GETDATE()
                    WHERE MaDatBan = {ma};
                END
                """);
        }
    }

    _db.SaveChanges();
    tx?.Commit();
}

    public List<DoanhThuNgay> DoanhThuTheoNgay(DateTime tu, DateTime den)
    {
        var tuNgay = tu.Date; var denNgay = den.Date.AddDays(1);
        return _db.DonHangs
            .Where(d => d.TrangThai == TrangThaiDon.DaThanhToan && d.NgayTao >= tuNgay && d.NgayTao < denNgay)
            .AsEnumerable()
            .GroupBy(d => d.NgayTao.Date)
            .Select(g => new DoanhThuNgay(g.Key, g.Count(), g.Sum(x => x.TongTien)))
            .OrderBy(x => x.Ngay).ToList();
    }

    public List<MonBanChay> MonBanChay(DateTime tu, DateTime den, int top = 10)
    {
        var tuNgay = tu.Date; var denNgay = den.Date.AddDays(1);
        return _db.ChiTietDonHangs.Include(c => c.MonAn).Include(c => c.DonHang)
            .Where(c => c.DonHang != null && c.DonHang.TrangThai == TrangThaiDon.DaThanhToan && c.DonHang.NgayTao >= tuNgay && c.DonHang.NgayTao < denNgay)
            .AsEnumerable()
            .GroupBy(c => c.MonAn?.TenMon ?? "Món khác")
            .Select(g => new MonBanChay(g.Key, g.Sum(x => x.SoLuong), g.Sum(x => x.ThanhTien)))
            .OrderByDescending(x => x.SoLuong).ThenByDescending(x => x.DoanhThu).Take(top).ToList();
    }
}
