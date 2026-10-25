using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using QLNH_Web.Data;
using QLNH_Web.Models;
using QLNH_Web.ViewModels;

namespace QLNH_Web.Services;

public class KetQuaDatBan
{
    public DatBan? DatBan { get; set; }
    public string? Loi { get; set; }
    public string? MaLoi { get; set; }      // "HET_HAN" khi phiên giữ chỗ đã hết hạn
    public bool EmailDaGui { get; set; }
}

public class DatBanService
{
    public const int KhachToiDa = 20;
    public const int ThoiGianGiuCho = 15;              // phút
    public const decimal CocCoDinh = 500_000m;
    public const decimal TyLeCoc30 = 0.30m;
    public const decimal TyLeVat = 0.08m;
    public const decimal TyLePhucVu = 0.05m;
    public static readonly string[] PhuongThucHopLe = { "VIETQR", "MOMO", "VNPAY", "THE" };
    public static readonly SlotOption[] Slots =
    {
        new("11:00", "11:00 - Bữa trưa"), new("12:00", "12:00 - Bữa trưa"), new("13:00", "13:00 - Bữa trưa"),
        new("17:30", "17:30 - Chiều muộn"), new("18:00", "18:00 - Buổi tối"), new("19:00", "19:00 - Buổi tối"),
        new("20:00", "20:00 - Buổi tối"), new("21:00", "21:00 - Buổi tối"),
    };

    // Chặn 2 yêu cầu cùng giành chỗ cuối cùng (đủ dùng cho 1 máy chủ)
    private static readonly SemaphoreSlim Gate = new(1, 1);

    private readonly AuthDbContext _db;
    private readonly QLNH.Data.AppDbContext _appDb;
    private readonly IMailSender _mail;

    public DatBanService(AuthDbContext db, QLNH.Data.AppDbContext appDb, IMailSender mail)
    {
        _db = db;
        _appDb = appDb;
        _mail = mail;
    }

    // ---------- Tính tiền (giống hệt công thức ở trình duyệt) ----------
    private static decimal Lam(decimal x) => Math.Round(x, 0, MidpointRounding.AwayFromZero);

    public static BaoGia TinhGia(decimal tongMon, decimal phuThu, UuDai? ud)
    {
        var goc = tongMon + phuThu;
        var vat = Lam(goc * TyLeVat);
        var pv = Lam(goc * TyLePhucVu);
        var tong = goc + vat + pv;
        decimal giam = 0;
        if (ud != null)
        {
            giam = Lam(tong * ud.PhanTramGiam / 100m);
            if (ud.GiamToiDa.HasValue) giam = Math.Min(giam, ud.GiamToiDa.Value);
            giam = Math.Min(giam, tong);
        }
        return new BaoGia(tongMon, phuThu, vat, pv, giam, tong - giam);
    }

    public static decimal TinhCoc(string che, decimal tongHoaDon) => che switch
    {
        CheDo.Ba30 => Lam(tongHoaDon * TyLeCoc30),
        CheDo.Tra100 => tongHoaDon,
        _ => CocCoDinh
    };

    public static bool TryParseNgayGio(string ngay, string gio, out DateTime kq)
        => DateTime.TryParseExact($"{ngay} {gio}", "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out kq);

    // ---------- Chỗ trống ----------
    // Mỗi bàn dùng bữa ~2 giờ: các đơn lệch nhau dưới 2 giờ tính là cùng lúc.
    // Đơn "Chờ đặt cọc" còn hạn giữ chỗ cũng được tính là đã chiếm chỗ.
    public async Task<List<KhaDungItem>> KiemTraKhaDungAsync(DateTime ngayGio, int soKhach, int? boQuaId = null)
    {
        var khuVucs = await _db.KhuVucs.AsNoTracking().Where(k => k.HoatDong).OrderBy(k => k.Id).ToListAsync();
        var tu = ngayGio.AddHours(-2);
        var den = ngayGio.AddHours(2);
        var bayGio = DateTime.Now;
        var daDat = await _db.DatBans.AsNoTracking()
            .Where(d => d.TrangThai != TrangThaiDatBan.DaHuy
                        && (d.TrangThai != TrangThaiDatBan.ChoCoc || d.GiuChoDen > bayGio)
                        && (boQuaId == null || d.Id != boQuaId)
                        && d.NgayGio > tu && d.NgayGio < den)
            .GroupBy(d => d.KhuVucId)
            .Select(g => new { KhuVucId = g.Key, Khach = g.Sum(x => x.SoKhach) })
            .ToListAsync();
        return khuVucs.Select(k =>
        {
            var used = daDat.FirstOrDefault(x => x.KhuVucId == k.Id)?.Khach ?? 0;
            var conLai = Math.Max(0, k.SucChua - used);
            return new KhaDungItem(k.Id, conLai, conLai >= soKhach);
        }).ToList();
    }

    /// <summary>Hủy các lần giữ chỗ đã quá hạn mà chưa xác nhận cọc.</summary>
    public async Task DonDepGiuChoAsync()
    {
        var bayGio = DateTime.Now;
        await _db.DatBans
            .Where(d => d.TrangThai == TrangThaiDatBan.ChoCoc && d.GiuChoDen < bayGio)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.TrangThai, TrangThaiDatBan.DaHuy)
                .SetProperty(d => d.LyDoHuy, "Quá hạn đặt cọc"));
    }

    // ---------- Bước 1 -> 2: giữ chỗ (tạo mới hoặc cập nhật lần giữ chỗ đang có) ----------
    public async Task<KetQuaDatBan> GiuChoAsync(DatBanRequest r, string userId)
    {
        if (!TryParseNgayGio(r.Ngay, r.Gio, out var ngayGio) || !Slots.Any(s => s.Value == r.Gio))
            return Loi("Ngày hoặc giờ không hợp lệ.");
        if (ngayGio < DateTime.Now.AddHours(1)) return Loi("Vui lòng đặt bàn trước giờ hẹn ít nhất 1 giờ.");
        if (ngayGio > DateTime.Now.AddDays(90)) return Loi("Chỉ nhận đặt bàn trong vòng 90 ngày tới.");
        if (!CheDo.TatCa.Contains(r.CheDoCoc)) return Loi("Chế độ đặt cọc không hợp lệ.");

        var chon = r.Mons.Where(m => m.SoLuong > 0)
            .GroupBy(m => m.MucThucDonId)
            .Select(g => new MonChonDto { MucThucDonId = g.Key, SoLuong = g.Sum(x => x.SoLuong) })
            .ToList();
        if (chon.Any(m => m.SoLuong > 50)) return Loi("Số lượng mỗi món tối đa 50.");

        await DonDepGiuChoAsync();

        await Gate.WaitAsync();
        try
        {
            DatBan? b = null;
            if (r.Id.HasValue)
            {
                b = await _db.DatBans.Include(x => x.Mons)
                    .FirstOrDefaultAsync(x => x.Id == r.Id && x.UserId == userId && x.TrangThai == TrangThaiDatBan.ChoCoc);
                if (b == null) return Loi("Phiên giữ chỗ đã hết hạn, vui lòng đặt lại.", "HET_HAN");
            }

            var kv = await _db.KhuVucs.FirstOrDefaultAsync(k => k.Id == r.KhuVucId && k.HoatDong);
            if (kv == null) return Loi("Khu vực ngồi không hợp lệ.");

            var kd = (await KiemTraKhaDungAsync(ngayGio, r.SoKhach, b?.Id)).First(x => x.KhuVucId == kv.Id);
            if (!kd.DuCho)
                return Loi(kd.ConLai == 0
                    ? $"Khu vực \"{kv.Ten}\" đã hết chỗ vào khung giờ này. Vui lòng chọn khu vực hoặc giờ khác."
                    : $"Khu vực \"{kv.Ten}\" chỉ còn {kd.ConLai} chỗ vào khung giờ này.");

            // Mã ưu đãi
            UuDai? ud = null;
            var ma = r.MaUuDai?.Trim().ToUpperInvariant();
            if (!string.IsNullOrEmpty(ma))
            {
                var homNay = DateTime.Today;
                ud = await _db.UuDais.FirstOrDefaultAsync(u => u.Ma == ma && u.HoatDong && (u.HanDung == null || u.HanDung >= homNay));
                if (ud == null) return Loi("Mã ưu đãi không hợp lệ hoặc đã hết hạn.");
            }

            // Giá luôn lấy từ CSDL, không tin giá do trình duyệt gửi lên
            var ids = chon.Select(m => m.MucThucDonId).ToList();
            var mons = await _db.MucThucDons.Where(m => ids.Contains(m.Id) && m.ConPhucVu).ToListAsync();
            var dong = new List<DatBanMon>();
            foreach (var c in chon)
            {
                var mon = mons.FirstOrDefault(m => m.Id == c.MucThucDonId);
                if (mon == null) return Loi("Có món không còn phục vụ, vui lòng tải lại trang.");
                dong.Add(new DatBanMon { MucThucDonId = mon.Id, TenMon = mon.Ten, DonGia = mon.Gia, SoLuong = c.SoLuong });
            }

            var gia = TinhGia(dong.Sum(d => d.DonGia * d.SoLuong), kv.PhuThu, ud);
            if (r.CheDoCoc != CheDo.CoDinh && gia.TongHoaDon <= 0)
                return Loi("Hãy chọn món trước để dùng chế độ cọc theo hóa đơn.");

            var moi = b == null;
            b ??= new DatBan { MaDatBan = await TaoMaAsync(), UserId = userId, TrangThai = TrangThaiDatBan.ChoCoc };
            b.HoTen = r.HoTen.Trim();
            b.SoDienThoai = r.SoDienThoai.Trim();
            b.Email = r.Email.Trim();
            b.NgayGio = ngayGio;
            b.SoKhach = r.SoKhach;
            b.KhuVucId = kv.Id;
            b.KhuVuc = kv;
            b.DipDacBiet = string.IsNullOrWhiteSpace(r.DipDacBiet) ? null : r.DipDacBiet.Trim();
            b.GhiChu = string.IsNullOrWhiteSpace(r.GhiChu) ? null : r.GhiChu.Trim();
            b.TongMon = gia.TongMon;
            b.PhuThuKhuVuc = gia.PhuThu;
            b.Vat = gia.Vat;
            b.PhiPhucVu = gia.PhiPhucVu;
            b.MaUuDai = ud?.Ma;
            b.GiamGia = gia.GiamGia;
            b.TongHoaDon = gia.TongHoaDon;
            b.CheDoCoc = r.CheDoCoc;
            b.TienCoc = TinhCoc(r.CheDoCoc, gia.TongHoaDon);
            b.ConLai = Math.Max(0, gia.TongHoaDon - b.TienCoc);
            b.GiuChoDen = DateTime.Now.AddMinutes(ThoiGianGiuCho);
            b.NgayCapNhat = moi ? null : DateTime.Now;

            if (moi) _db.DatBans.Add(b);
            else foreach (var cu in b.Mons.ToList()) _db.DatBanMons.Remove(cu);
            foreach (var d in dong) b.Mons.Add(d);

            await _db.SaveChangesAsync();
            return new KetQuaDatBan { DatBan = b };
        }
        finally { Gate.Release(); }
    }

    // ---------- Bước 2 -> 3: xác nhận đã thanh toán cọc ----------
    public async Task<KetQuaDatBan> ThanhToanAsync(int id, string userId, ThanhToanRequest r)
    {
        if (!PhuongThucHopLe.Contains(r.PhuongThuc)) return Loi("Phương thức thanh toán không hợp lệ.");
        if (!CheDo.TatCa.Contains(r.CheDoCoc)) return Loi("Chế độ đặt cọc không hợp lệ.");

        await DonDepGiuChoAsync();
        var b = await _db.DatBans.Include(x => x.KhuVuc).Include(x => x.Mons)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (b == null) return Loi("Không tìm thấy đơn đặt bàn.");
        if (b.TrangThai == TrangThaiDatBan.DaHuy) return Loi("Phiên giữ chỗ đã hết hạn, vui lòng đặt lại.", "HET_HAN");
        if (b.TrangThai != TrangThaiDatBan.ChoCoc) return Loi("Đơn này đã được xác nhận thanh toán cọc.");
        if (r.CheDoCoc != CheDo.CoDinh && b.TongHoaDon <= 0) return Loi("Hãy chọn món trước để dùng chế độ cọc theo hóa đơn.");

        b.CheDoCoc = r.CheDoCoc;
        b.TienCoc = TinhCoc(r.CheDoCoc, b.TongHoaDon);
        b.ConLai = Math.Max(0, b.TongHoaDon - b.TienCoc);
        b.PhuongThucThanhToan = r.PhuongThuc;
        b.NganHang = r.PhuongThuc == "VIETQR" && (r.NganHang == "VCB" || r.NganHang == "MB") ? r.NganHang : null;
        b.MaGiaoDich = "GD" + DateTime.Now.ToString("yyMMddHHmmss") + RandomCode(4);
        b.DaThanhToanCoc = true;
        b.NgayThanhToan = DateTime.Now;
        b.GiuChoDen = null;
        b.TrangThai = TrangThaiDatBan.ChoDuyet;
        b.NgayCapNhat = DateTime.Now;
        await _db.SaveChangesAsync();
        await DongBoSangWinFormsAsync(b);

        var sent = await _mail.SendAsync(b.Email, $"[Hương Việt] Đã nhận đặt bàn {b.MaDatBan}", NoiDungEmail(b, "Chúng tôi đã ghi nhận đặt cọc của bạn. Nhà hàng sẽ đối chiếu giao dịch và xác nhận trong thời gian sớm nhất."));
        return new KetQuaDatBan { DatBan = b, EmailDaGui = sent };
    }

    // ---------- Hủy / duyệt / hoàn thành ----------
    /// <summary>Trả về null nếu thành công, ngược lại là thông báo lỗi.</summary>
    public async Task<string?> HuyAsync(int id, string? userId, bool laAdmin, string? lyDo)
    {
        var b = await _db.DatBans.Include(x => x.KhuVuc).Include(x => x.Mons).FirstOrDefaultAsync(x => x.Id == id);
        if (b == null) return "Không tìm thấy đơn đặt bàn.";
        if (!laAdmin && b.UserId != userId) return "Bạn không có quyền hủy đơn này.";
        if (b.TrangThai is TrangThaiDatBan.DaHuy or TrangThaiDatBan.HoanThanh) return "Đơn này không thể hủy.";
        if (!laAdmin && b.TrangThai != TrangThaiDatBan.ChoCoc && b.NgayGio < DateTime.Now.AddHours(2))
            return "Chỉ có thể tự hủy trước giờ hẹn ít nhất 2 giờ. Vui lòng liên hệ nhà hàng.";

        var daCoc = b.DaThanhToanCoc;
        b.TrangThai = TrangThaiDatBan.DaHuy;
        b.LyDoHuy = string.IsNullOrWhiteSpace(lyDo) ? (laAdmin ? "Nhà hàng hủy đơn" : "Khách hủy đơn") : lyDo.Trim();
        b.NgayCapNhat = DateTime.Now;
        await _db.SaveChangesAsync();
        await DongBoSangWinFormsAsync(b);
        if (daCoc) await _mail.SendAsync(b.Email, $"[Hương Việt] Đơn đặt bàn {b.MaDatBan} đã hủy", NoiDungEmail(b, "Đơn đặt bàn của bạn đã được hủy."));
        return null;
    }

    public async Task<string?> DuyetAsync(int id)
    {
        var b = await _db.DatBans.Include(x => x.KhuVuc).Include(x => x.Mons).FirstOrDefaultAsync(x => x.Id == id);
        if (b == null) return "Không tìm thấy đơn đặt bàn.";
        if (b.TrangThai != TrangThaiDatBan.ChoDuyet) return "Chỉ duyệt được đơn đang chờ duyệt.";
        b.TrangThai = TrangThaiDatBan.DaXacNhan;
        b.NgayCapNhat = DateTime.Now;
        await _db.SaveChangesAsync();
        await DongBoSangWinFormsAsync(b);
        await _mail.SendAsync(b.Email, $"[Hương Việt] Đặt bàn {b.MaDatBan} đã được xác nhận", NoiDungEmail(b, "Nhà hàng đã xác nhận đặt bàn của bạn. Vui lòng xuất trình mã khi đến."));
        return null;
    }

    public async Task<string?> HoanThanhAsync(int id)
    {
        var b = await _db.DatBans.Include(x => x.KhuVuc).Include(x => x.Mons).FirstOrDefaultAsync(x => x.Id == id);
        if (b == null) return "Không tìm thấy đơn đặt bàn.";
        if (b.TrangThai != TrangThaiDatBan.DaXacNhan) return "Chỉ hoàn thành được đơn đã xác nhận.";
        b.TrangThai = TrangThaiDatBan.HoanThanh;
        b.NgayCapNhat = DateTime.Now;
        await _db.SaveChangesAsync();
        await DongBoSangWinFormsAsync(b);
        return null;
    }

    // ---------- Đồng bộ sang bảng của WinForms (QLNH.Admin) ----------
    public async Task DongBoSangWinFormsAsync(DatBan b)
    {
        var kvTen = b.KhuVuc?.Ten
            ?? await _db.KhuVucs.Where(k => k.Id == b.KhuVucId).Select(k => k.Ten).FirstOrDefaultAsync()
            ?? "Khu vực Web";
        var mons = b.Mons != null && b.Mons.Count > 0
            ? b.Mons
            : await _db.DatBanMons.Where(m => m.DatBanId == b.Id).ToListAsync();

        var bans = await _appDb.Bans.OrderBy(x => x.SoGhe).ToListAsync();
        var ban = bans.FirstOrDefault(x => x.TrangThai == "Trống" && x.SoGhe >= b.SoKhach)
                  ?? bans.LastOrDefault(x => x.TrangThai == "Trống")
                  ?? bans.FirstOrDefault(x => x.SoGhe >= b.SoKhach)
                  ?? bans.LastOrDefault();

        var ttDatBanWin = b.TrangThai switch
        {
            TrangThaiDatBan.DaXacNhan => "Đã xác nhận",
            TrangThaiDatBan.HoanThanh => "Đã đến",
            TrangThaiDatBan.DaHuy => "Đã hủy",
            _ => "Chờ xác nhận"
        };
        var ttDonWin = b.TrangThai switch
        {
            TrangThaiDatBan.DaXacNhan => QLNH.Data.Entities.TrangThaiDon.DangCheBien,
            TrangThaiDatBan.HoanThanh => QLNH.Data.Entities.TrangThaiDon.DaThanhToan,
            TrangThaiDatBan.DaHuy => QLNH.Data.Entities.TrangThaiDon.DaHuy,
            _ => QLNH.Data.Entities.TrangThaiDon.Moi
        };

        var prefix = $"[{b.MaDatBan}]";
        var winDatBan = await _appDb.DatBans.FirstOrDefaultAsync(d => d.GhiChu != null && d.GhiChu.StartsWith(prefix));
        var dip = string.IsNullOrWhiteSpace(b.DipDacBiet) ? "" : $" | {b.DipDacBiet}";
        var ghiChuKhach = string.IsNullOrWhiteSpace(b.GhiChu) ? "" : $" | {b.GhiChu}";
        var ghiChuDatBan = $"{prefix} {kvTen}{dip} | Cọc: {b.TienCoc:N0}đ{ghiChuKhach}";
        if (ghiChuDatBan.Length > 300) ghiChuDatBan = ghiChuDatBan[..300];

        if (winDatBan == null)
        {
            winDatBan = new QLNH.Data.Entities.DatBan
            {
                TenKhach = b.HoTen,
                SoDienThoai = b.SoDienThoai,
                NgayGio = b.NgayGio,
                SoNguoi = Math.Clamp(b.SoKhach, 1, 1000),
                BanId = ban?.Id,
                TrangThai = ttDatBanWin,
                GhiChu = ghiChuDatBan
            };
            _appDb.DatBans.Add(winDatBan);
        }
        else
        {
            winDatBan.TenKhach = b.HoTen;
            winDatBan.SoDienThoai = b.SoDienThoai;
            winDatBan.NgayGio = b.NgayGio;
            winDatBan.SoNguoi = Math.Clamp(b.SoKhach, 1, 1000);
            winDatBan.BanId ??= ban?.Id;
            winDatBan.TrangThai = ttDatBanWin;
            winDatBan.GhiChu = ghiChuDatBan;
        }

        var winDon = await _appDb.DonHangs.Include(d => d.ChiTiets)
            .FirstOrDefaultAsync(d => d.GhiChu != null && d.GhiChu.StartsWith(prefix));
        var ghiChuDon = $"{prefix} {kvTen} ({b.SoKhach} khách{(b.SoBan > 1 ? $", {b.SoBan} bàn" : "")}) | Cọc: {b.TienCoc:N0}đ | Còn lại: {b.ConLai:N0}đ";
        if (ghiChuDon.Length > 300) ghiChuDon = ghiChuDon[..300];

        if (winDon == null && (mons.Count > 0 || b.TongHoaDon > 0))
        {
            winDon = new QLNH.Data.Entities.DonHang
            {
                NgayTao = b.NgayThanhToan ?? b.NgayTao,
                TenKhach = b.HoTen,
                SoDienThoai = b.SoDienThoai,
                BanId = winDatBan.BanId ?? ban?.Id,
                TrangThai = ttDonWin,
                TongTien = b.TongHoaDon,
                GhiChu = ghiChuDon
            };

            var soBan = Math.Max(1, b.SoBan);
            var allMonAns = await _appDb.MonAns.ToListAsync();
            var defaultDanhMucId = await _appDb.DanhMucs.Select(d => d.Id).FirstOrDefaultAsync();
            if (defaultDanhMucId == 0)
            {
                var dm = new QLNH.Data.Entities.DanhMuc { TenDanhMuc = "Thực đơn Web", MoTa = "Món đồng bộ từ Web" };
                _appDb.DanhMucs.Add(dm);
                await _appDb.SaveChangesAsync();
                defaultDanhMucId = dm.Id;
            }

            foreach (var m in mons)
            {
                QLNH.Data.Entities.MonAn? monAn = null;
                if (m.MonAnId.HasValue)
                    monAn = allMonAns.FirstOrDefault(x => x.Id == m.MonAnId.Value);
                monAn ??= allMonAns.FirstOrDefault(x => string.Equals(x.TenMon, m.TenMon, StringComparison.OrdinalIgnoreCase));

                if (monAn == null)
                {
                    monAn = new QLNH.Data.Entities.MonAn
                    {
                        TenMon = m.TenMon,
                        Gia = m.DonGia,
                        DanhMucId = defaultDanhMucId,
                        ConPhucVu = true
                    };
                    _appDb.MonAns.Add(monAn);
                    await _appDb.SaveChangesAsync();
                    allMonAns.Add(monAn);
                }

                winDon.ChiTiets.Add(new QLNH.Data.Entities.ChiTietDonHang
                {
                    MonAnId = monAn.Id,
                    DonGia = m.DonGia,
                    SoLuong = Math.Max(1, m.SoLuong * soBan)
                });
            }

            _appDb.DonHangs.Add(winDon);
        }
        else if (winDon != null)
        {
            winDon.TrangThai = ttDonWin;
            winDon.TongTien = b.TongHoaDon;
            winDon.GhiChu = ghiChuDon;
        }

        await _appDb.SaveChangesAsync();
    }

    public async Task DongBoTaiKhoanSangWinFormsAsync(AppUser u, string? plainPassword = null, bool? hoatDong = null, bool laAdmin = false)
    {
        var tenDangNhap = (u.Email ?? u.UserName ?? u.PhoneNumber ?? "").Trim();
        if (string.IsNullOrEmpty(tenDangNhap)) return;
        if (tenDangNhap.Length > 50) tenDangNhap = tenDangNhap[..50];

        var roleName = laAdmin ? "Admin" : "KhachHang";
        var role = await _appDb.VaiTros.FirstOrDefaultAsync(v => v.TenVaiTro == roleName);
        if (role == null)
        {
            role = new QLNH.Data.Entities.VaiTro { TenVaiTro = roleName };
            _appDb.VaiTros.Add(role);
            await _appDb.SaveChangesAsync();
        }

        var tk = await _appDb.TaiKhoans.FirstOrDefaultAsync(t => t.TenDangNhap == tenDangNhap);
        var active = hoatDong ?? !(u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow);
        if (tk == null)
        {
            tk = new QLNH.Data.Entities.TaiKhoan
            {
                TenDangNhap = tenDangNhap,
                HoTen = string.IsNullOrWhiteSpace(u.HoTen) ? tenDangNhap : u.HoTen.Trim(),
                MatKhauHash = QLNH.Data.Services.PasswordHasher.Hash(plainPassword ?? "Khach@123"),
                VaiTroId = role.Id,
                HoatDong = active
            };
            _appDb.TaiKhoans.Add(tk);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(u.HoTen)) tk.HoTen = u.HoTen.Trim();
            if (!string.IsNullOrEmpty(plainPassword)) tk.MatKhauHash = QLNH.Data.Services.PasswordHasher.Hash(plainPassword);
            if (hoatDong.HasValue) tk.HoatDong = hoatDong.Value;
        }
        await _appDb.SaveChangesAsync();
    }

    // ---------- Helpers ----------
    private static KetQuaDatBan Loi(string msg, string? ma = null) => new() { Loi = msg, MaLoi = ma };

    private async Task<string> TaoMaAsync()
    {
        while (true)
        {
            var ma = "HV-" + RandomNumberGenerator.GetInt32(100000, 1000000);
            if (!await _db.DatBans.AnyAsync(d => d.MaDatBan == ma)) return ma;
        }
    }

    private static string RandomCode(int len)
    {
        const string abc = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var chars = new char[len];
        for (int i = 0; i < len; i++) chars[i] = abc[RandomNumberGenerator.GetInt32(abc.Length)];
        return new string(chars);
    }

    private static string NoiDungEmail(DatBan b, string loiNhan)
    {
        string E(string? s) => WebUtility.HtmlEncode(s ?? "");
        return $@"<div style=""font-family:Segoe UI,Arial,sans-serif;max-width:520px"">
<h2 style=""color:#b3261e"">Nhà hàng Hương Việt</h2>
<p>Xin chào <b>{E(b.HoTen)}</b>,</p><p>{E(loiNhan)}</p>
<table cellpadding=""6"" style=""border-collapse:collapse"">
<tr><td>Mã đặt bàn</td><td><b>{E(b.MaDatBan)}</b></td></tr>
<tr><td>Thời gian</td><td>{b.NgayGio:HH:mm dd/MM/yyyy}</td></tr>
<tr><td>Số khách</td><td>{b.SoKhach}</td></tr>
<tr><td>Khu vực</td><td>{E(b.KhuVuc?.Ten)}</td></tr>
<tr><td>Tổng hóa đơn ước tính</td><td>{b.TongHoaDon:N0} đ</td></tr>
<tr><td>Tiền cọc đã thanh toán</td><td>{b.TienCoc:N0} đ</td></tr>
<tr><td>Còn lại trả tại nhà hàng</td><td>{b.ConLai:N0} đ</td></tr>
<tr><td>Trạng thái</td><td><b>{E(b.TrangThai)}</b></td></tr>
</table></div>";
    }
}
