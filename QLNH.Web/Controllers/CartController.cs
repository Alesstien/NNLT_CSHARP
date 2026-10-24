using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNH.Data;            // TODO: namespace của AppDbContext (đổi cho đúng project của bạn)
using QLNH_Web.Data;        // TODO: namespace của AuthDbContext (đổi cho đúng project của bạn)
using QLNH_Web.Models;

namespace QLNH_Web.Controllers;


public class CartController : Controller
{
    private readonly AppDbContext _db;
    private readonly AuthDbContext _auth;

    public CartController(AppDbContext db, AuthDbContext auth)
    {
        _db = db;
        _auth = auth;
    }

    // Thêm món ăn vào giỏ hàng
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int id, int soLuong = 1)
    {
        if (id <= 0)
            return BadRequest("Mã món ăn không hợp lệ.");

        var mon = await _db.MonAns
            .FirstOrDefaultAsync(m => m.Id == id && m.ConPhucVu);

        if (mon == null)
            return NotFound("Món ăn không tồn tại hoặc đã ngừng phục vụ.");

        soLuong = Math.Clamp(soLuong, 1, 100);

        var cart = CartHelper.Get(HttpContext.Session);

        if (cart.TryGetValue(id, out var soLuongCu))
            cart[id] = Math.Min(soLuongCu + soLuong, 100);
        else
            cart[id] = soLuong;

        CartHelper.Save(HttpContext.Session, cart);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Ok(new
            {
                success = true,
                message = "Đã thêm món vào giỏ hàng.",
                cartCount = cart.Values.Sum()
            });
        }

        TempData["Msg"] = "Đã thêm món vào giỏ hàng.";
        return RedirectToAction("Index", "Menu");
    }


    // ---------- Hàm dùng chung ----------
    private async Task<List<CartLine>> LoadLines()
    {
        var cart = CartHelper.Get(HttpContext.Session);
        var ids = cart.Keys.ToList();
        // TODO: đổi m.Id / m.Ten / m.Gia theo entity MonAn của bạn
        var mons = await _db.MonAns.Where(m => ids.Contains(m.Id)).ToListAsync();
        return mons.Select(m => new CartLine
        {
            MonAnId = m.Id,
            TenMon = m.TenMon,
            DonGia = m.Gia,
            SoLuong = cart[m.Id]
        }).ToList();
    }

    /// <summary>Chọn khu vực hợp lệ: tiệc thì chọn sảnh tiệc, ăn thường thì chọn khu thường.</summary>
    private static KhuVuc? ChonKhuVuc(BookingDraft b, List<KhuVuc> kvs)
    {
        var hopLe = kvs.Where(k => k.ChoTiec == b.LaTiec).ToList();
        var kv = hopLe.FirstOrDefault(k => k.Id == b.KhuVucId) ?? hopLe.FirstOrDefault();
        if (kv != null) b.KhuVucId = kv.Id;
        return kv;
    }

    // ---------- Bước 1: giỏ hàng + đặt bàn ----------
    public async Task<IActionResult> Index()
    {
        var b = BookingDraft.Get(HttpContext.Session);
        b.ThoiGianDen ??= DateTime.Now.AddHours(2);
        var kvs = await _auth.KhuVucs.Where(k => k.HoatDong).OrderBy(k => k.Id).ToListAsync();
        var kv = ChonKhuVuc(b, kvs);
        var lines = await LoadLines();

        var vm = new CartPageVm { Lines = lines, Booking = b, KhuVucs = kvs, KhuVuc = kv };
        if (kv != null && lines.Count > 0)
            vm.Bill = Bill.Tinh(lines.Sum(l => l.ThanhTien), b, kv, null, CheDo.CoDinh);
        return View(vm);
    }

    /// <summary>Mọi nút trong form giỏ hàng (+, −, xóa, đổi loại lễ, tiếp tục) đều gửi vào đây.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(BookingDraft booking, string? op)
    {
        if (op == "ng+") booking.SoNguoi++;
        else if (op == "ng-") booking.SoNguoi--;
        booking.SoNguoi = Math.Clamp(booking.SoNguoi, 1, 1000);
        if (!BookingDraft.CacLoaiLe.Contains(booking.LoaiLe)) booking.LoaiLe = BookingDraft.CacLoaiLe[0];
        booking.Save(HttpContext.Session);

        var cart = CartHelper.Get(HttpContext.Session);
        if (op == "next") return await TiepTuc(booking, cart);

        var p = (op ?? "").Split(':');   // "inc:5" | "dec:5" | "del:5"
        if (p.Length == 2 && int.TryParse(p[1], out var id) && cart.ContainsKey(id))
        {
            switch (p[0])
            {
                case "inc": cart[id]++; break;
                case "dec": if (--cart[id] <= 0) cart.Remove(id); break;
                case "del": cart.Remove(id); break;
            }
            CartHelper.Save(HttpContext.Session, cart);
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> TiepTuc(BookingDraft b, Dictionary<int, int> cart)
    {
        string? loi = null;
        var kvs = await _auth.KhuVucs.Where(k => k.HoatDong).ToListAsync();
        var kv = ChonKhuVuc(b, kvs);

        if (cart.Count == 0) loi = "Giỏ hàng đang trống.";
        else if (b.ThoiGianDen == null || b.ThoiGianDen <= DateTime.Now) loi = "Hãy chọn ngày giờ nhận bàn trong tương lai.";
        else if (kv == null) loi = "Chưa có khu vực phù hợp với loại buổi này.";
        else if (b.SoNguoi > kv.SucChua) loi = $"{kv.Ten} chỉ chứa tối đa {kv.SucChua} khách.";

        if (loi != null) { TempData["Loi"] = loi; return RedirectToAction(nameof(Index)); }
        b.Save(HttpContext.Session);
        return RedirectToAction(nameof(Payment));
    }

    // ---------- Bước 2: đặt cọc & thanh toán ----------
    private async Task<PaymentVm?> BuildPaymentVm(PaymentInput inp)
    {
        var lines = await LoadLines();
        var b = BookingDraft.Get(HttpContext.Session);
        if (lines.Count == 0 || b.ThoiGianDen == null) return null;

        var kvs = await _auth.KhuVucs.Where(k => k.HoatDong).ToListAsync();
        var kv = ChonKhuVuc(b, kvs);
        if (kv == null) return null;

        var maGd = HttpContext.Session.GetString("ma_gd");
        if (!DateTime.TryParse(HttpContext.Session.GetString("giu_cho"), null, DateTimeStyles.RoundtripKind, out var hetHan)
            || string.IsNullOrEmpty(maGd)) return null;

        if (!CheDo.TatCa.Contains(inp.CheDoCoc)) inp.CheDoCoc = CheDo.CoDinh;
        if (!new[] { "VIETQR", "MOMO", "VNPAY", "THE" }.Contains(inp.PhuongThuc)) inp.PhuongThuc = "VIETQR";
        if (inp.NganHang != "MB") inp.NganHang = "VCB";

        UuDai? ud = null; string? maMsg = null;
        var ma = inp.Ma?.Trim().ToUpper();
        if (!string.IsNullOrEmpty(ma))
        {
            ud = await _auth.UuDais.FirstOrDefaultAsync(u => u.Ma == ma && u.HoatDong && (u.HanDung == null || u.HanDung >= DateTime.Today));
            maMsg = ud == null
                ? "Mã ưu đãi không hợp lệ hoặc đã hết hạn."
                : $"Đã áp dụng {ma}: giảm {ud.PhanTramGiam}% tiền món" + (ud.GiamToiDa is decimal t ? $", tối đa {t:N0} đ." : ".");
        }

        return new PaymentVm
        {
            Input = inp, Lines = lines, Booking = b, KhuVuc = kv,
            Bill = Bill.Tinh(lines.Sum(l => l.ThanhTien), b, kv, ud, inp.CheDoCoc),
            MaMsg = maMsg, MaHopLe = ud != null, MaGiaoDich = maGd, GiuChoDen = hetHan
        };
    }

    [Authorize, HttpGet]
    public async Task<IActionResult> Payment()
    {
        HttpContext.Session.SetString("giu_cho", DateTime.Now.AddMinutes(15).ToString("o"));
        HttpContext.Session.SetString("ma_gd", "HV" + DateTime.Now.ToString("yyMMddHHmmss"));
        var vm = await BuildPaymentVm(new PaymentInput());
        return vm == null ? RedirectToAction(nameof(Index)) : View(vm);
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Payment([Bind(Prefix = "Input")] PaymentInput inp, string? op)
    {
        if (op != "pay") ModelState.Clear();            // "Áp dụng mã" / đổi cọc / đổi phương thức: chỉ tính lại
        var vm = await BuildPaymentVm(inp);
        if (vm == null) return RedirectToAction(nameof(Index));
        if (op != "pay") return View(vm);

        if (!ModelState.IsValid) return View(vm);
        if (vm.GiuChoDen < DateTime.Now)
        {
            ModelState.AddModelError("", "Đã hết thời gian giữ chỗ. Vui lòng quay lại giỏ hàng để đặt lại.");
            return View(vm);
        }

        var b = vm.Booking; var bill = vm.Bill;
        var datBan = new DatBan
        {
            MaDatBan = "HV-" + Random.Shared.Next(100000, 999999),
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            HoTen = inp.HoTen.Trim(), SoDienThoai = inp.SoDienThoai.Trim(), Email = inp.Email.Trim(),
            NgayGio = b.ThoiGianDen!.Value, SoKhach = b.SoNguoi, SoBan = bill.SoBan,
            KhuVucId = vm.KhuVuc.Id, DipDacBiet = b.LoaiLe, GhiChu = b.GhiChu,
            TongMon = bill.TienMon, PhuThuKhuVuc = bill.PhuThu, Vat = bill.Vat, PhiPhucVu = bill.PhiPhucVu,
            MaUuDai = vm.MaHopLe ? inp.Ma!.Trim().ToUpper() : null, GiamGia = bill.Giam, TongHoaDon = bill.Tong,
            CheDoCoc = inp.CheDoCoc, TienCoc = bill.Coc, ConLai = bill.ConLai,
            PhuongThucThanhToan = inp.PhuongThuc,
            NganHang = inp.PhuongThuc == "VIETQR" ? inp.NganHang : null,
            MaGiaoDich = vm.MaGiaoDich,
            DaThanhToanCoc = true, NgayThanhToan = DateTime.Now,
            TrangThai = TrangThaiDatBan.ChoDuyet,     // admin đối chiếu tiền rồi chuyển sang "Đã xác nhận"
            Mons = vm.Lines.Select(l => new DatBanMon
            {
                MonAnId = l.MonAnId, TenMon = l.TenMon, DonGia = l.DonGia, SoLuong = l.SoLuong
            }).ToList()
        };
        _auth.DatBans.Add(datBan);
        await _auth.SaveChangesAsync();

        CartHelper.Clear(HttpContext.Session);
        BookingDraft.Clear(HttpContext.Session);
        return RedirectToAction(nameof(Success), new { id = datBan.Id });
    }

    // ---------- Bước 3: phiếu xác nhận ----------
    [Authorize]
    public async Task<IActionResult> Success(int id)
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var d = await _auth.DatBans.Include(x => x.KhuVuc).Include(x => x.Mons)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);
        return d == null ? NotFound() : View(d);
    }
}
