using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QLNH_Web.Models;
using QLNH_Web.Services;
using QLNH_Web.ViewModels;

namespace QLNH_Web.Controllers;

[ApiController, Route("api/dat-ban")]
public class DatBanApiController : ControllerBase
{
    private readonly DatBanService _svc;
    private readonly UserManager<AppUser> _users;

    public DatBanApiController(DatBanService svc, UserManager<AppUser> users) { _svc = svc; _users = users; }

    // GET /api/dat-ban/kha-dung?ngay=2026-10-17&gio=19:00&khach=2
    [HttpGet("kha-dung")]
    public async Task<IActionResult> KhaDung([FromQuery] string ngay, [FromQuery] string gio, [FromQuery] int khach)
    {
        if (!DatBanService.TryParseNgayGio(ngay, gio, out var dt)) return BadRequest(new { loi = "Ngày giờ không hợp lệ." });
        await _svc.DonDepGiuChoAsync();
        return Ok(await _svc.KiemTraKhaDungAsync(dt, Math.Clamp(khach, 1, DatBanService.KhachToiDa)));
    }

    // Bước 1 -> 2 (và khi áp mã ưu đãi): giữ chỗ 15 phút, trả về bảng chi phí do máy chủ tính
    [HttpPost("giu-cho"), ValidateAntiForgeryToken]
    public async Task<IActionResult> GiuCho([FromBody] DatBanRequest r)
    {
        if (User.Identity?.IsAuthenticated != true) return Unauthorized(new { loi = "Vui lòng đăng nhập để đặt bàn." });
        var kq = await _svc.GiuChoAsync(r, _users.GetUserId(User)!);
        if (kq.DatBan == null) return BadRequest(new { loi = kq.Loi, maLoi = kq.MaLoi });
        var b = kq.DatBan;
        return Ok(new
        {
            id = b.Id,
            maDatBan = b.MaDatBan,
            tongMon = b.TongMon,
            phuThu = b.PhuThuKhuVuc,
            vat = b.Vat,
            phiPhucVu = b.PhiPhucVu,
            giamGia = b.GiamGia,
            maUuDai = b.MaUuDai,
            tongHoaDon = b.TongHoaDon,
            cheDoCoc = b.CheDoCoc,
            tienCoc = b.TienCoc,
            conLai = b.ConLai,
            giuChoDen = b.GiuChoDen?.ToString("HH:mm")
        });
    }

    // Bước 2 -> 3: khách xác nhận đã thanh toán cọc
    [HttpPost("{id:int}/thanh-toan"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ThanhToan(int id, [FromBody] ThanhToanRequest r)
    {
        if (User.Identity?.IsAuthenticated != true) return Unauthorized(new { loi = "Vui lòng đăng nhập." });
        var kq = await _svc.ThanhToanAsync(id, _users.GetUserId(User)!, r);
        if (kq.DatBan == null) return BadRequest(new { loi = kq.Loi, maLoi = kq.MaLoi });
        var b = kq.DatBan;
        return Ok(new
        {
            maDatBan = b.MaDatBan,
            hoTen = b.HoTen,
            soDienThoai = b.SoDienThoai,
            email = b.Email,
            ngay = b.NgayGio.ToString("yyyy-MM-dd"),
            gio = b.NgayGio.ToString("HH:mm"),
            khuVuc = b.KhuVuc?.Ten,
            soKhach = b.SoKhach,
            tienCoc = b.TienCoc,
            conLai = b.ConLai,
            phuongThuc = b.PhuongThucThanhToan,
            trangThai = b.TrangThai,
            emailDaGui = kq.EmailDaGui
        });
    }

    [HttpPost("{id:int}/huy"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Huy(int id, [FromBody] HuyRequest r)
    {
        if (User.Identity?.IsAuthenticated != true) return Unauthorized(new { loi = "Vui lòng đăng nhập." });
        var loi = await _svc.HuyAsync(id, _users.GetUserId(User), User.IsInRole(AccountController.RoleAdmin), r.LyDo);
        return loi == null ? Ok(new { ok = true }) : BadRequest(new { loi });
    }
}
