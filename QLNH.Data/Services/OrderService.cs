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
        if (!TrangThaiDon.TatCa.Contains(trangThai)) throw new ArgumentException("Trạng thái không hợp lệ.");
        var don = _db.DonHangs.Find(donId) ?? throw new InvalidOperationException("Không tìm thấy đơn hàng.");
        don.TrangThai = trangThai;
        if ((trangThai == TrangThaiDon.DaThanhToan || trangThai == TrangThaiDon.DaHuy) && don.BanId != null)
        {
            var ban = _db.Bans.Find(don.BanId);
            if (ban != null) ban.TrangThai = "Trống";
        }
        _db.SaveChanges();
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
            .Where(c => c.DonHang!.TrangThai == TrangThaiDon.DaThanhToan && c.DonHang.NgayTao >= tuNgay && c.DonHang.NgayTao < denNgay)
            .AsEnumerable()
            .GroupBy(c => c.MonAn!.TenMon)
            .Select(g => new MonBanChay(g.Key, g.Sum(x => x.SoLuong), g.Sum(x => x.ThanhTien)))
            .OrderByDescending(x => x.SoLuong).Take(top).ToList();
    }
}
