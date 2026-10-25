using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNH_Web.Data;
using QLNH_Web.Models;
using QLNH_Web.Services;
using QLNH_Web.ViewModels;

namespace QLNH_Web.Controllers;

[Authorize(Roles = AccountController.RoleAdmin)]
public class AdminController : Controller
{
    private readonly AuthDbContext _db;
    private readonly UserManager<AppUser> _users;
    private readonly DatBanService _svc;

    public AdminController(AuthDbContext db, UserManager<AppUser> users, DatBanService svc)
    { _db = db; _users = users; _svc = svc; }

    // ---------- Tài khoản khách: xem trạng thái, khóa / mở khóa ----------
    [HttpGet]
    public async Task<IActionResult> Accounts(string? q)
    {
        var query = _db.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(u => u.Email!.Contains(q) || u.HoTen.Contains(q) || u.PhoneNumber!.Contains(q));
        }
        var users = await query.OrderByDescending(u => u.NgayTao).ToListAsync();

        var adminRoleId = await _db.Roles.Where(r => r.Name == AccountController.RoleAdmin).Select(r => r.Id).FirstOrDefaultAsync();
        var adminIds = (await _db.UserRoles.Where(ur => ur.RoleId == adminRoleId).Select(ur => ur.UserId).ToListAsync()).ToHashSet();
        var soDon = await _db.DatBans.Where(d => d.UserId != null).GroupBy(d => d.UserId!)
            .Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N);

        var now = DateTimeOffset.UtcNow;
        var rows = users.Select(u =>
        {
            var khoa = u.LockoutEnd != null && u.LockoutEnd > now;
            var khoaAdmin = khoa && u.LockoutEnd > now.AddYears(50);
            return new AccountRow
            {
                Id = u.Id, HoTen = u.HoTen, Email = u.Email ?? "", SoDienThoai = u.PhoneNumber ?? "",
                NgayTao = u.NgayTao, LanDangNhapCuoi = u.LanDangNhapCuoi,
                DaKhoa = khoa, LaAdmin = adminIds.Contains(u.Id),
                TrangThai = !khoa ? "Hoạt động" : khoaAdmin ? "Đã khóa" : "Tạm khóa (sai mật khẩu)",
                SoLanDatBan = soDon.TryGetValue(u.Id, out var n) ? n : 0
            };
        }).ToList();

        ViewBag.Q = q;
        return View(rows);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Khoa(string id)
    {
        var u = await _users.FindByIdAsync(id);
        if (u == null) return NotFound();
        if (u.Id == _users.GetUserId(User)) { TempData["Err"] = "Không thể tự khóa tài khoản của chính bạn."; return RedirectToAction(nameof(Accounts)); }
        if (await _users.IsInRoleAsync(u, AccountController.RoleAdmin)) { TempData["Err"] = "Không thể khóa tài khoản quản trị."; return RedirectToAction(nameof(Accounts)); }

        await _users.SetLockoutEnabledAsync(u, true);
        await _users.SetLockoutEndDateAsync(u, DateTimeOffset.MaxValue);
        await _users.UpdateSecurityStampAsync(u);   // buộc phiên đăng nhập hiện tại hết hiệu lực
        await _svc.DongBoTaiKhoanSangWinFormsAsync(u, null, false);
        TempData["Msg"] = $"Đã khóa tài khoản {u.Email}.";
        return RedirectToAction(nameof(Accounts));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MoKhoa(string id)
    {
        var u = await _users.FindByIdAsync(id);
        if (u == null) return NotFound();
        await _users.SetLockoutEndDateAsync(u, null);
        await _users.ResetAccessFailedCountAsync(u);
        await _svc.DongBoTaiKhoanSangWinFormsAsync(u, null, true);
        TempData["Msg"] = $"Đã mở khóa tài khoản {u.Email}.";
        return RedirectToAction(nameof(Accounts));
    }

    // ---------- Đơn đặt bàn ----------
    [HttpGet]
    public async Task<IActionResult> Reservations(string? trangThai)
    {
        await _svc.DonDepGiuChoAsync();
        var q = _db.DatBans.AsNoTracking().Include(d => d.KhuVuc).Include(d => d.Mons)
            .Where(d => d.TrangThai != TrangThaiDatBan.ChoCoc).AsQueryable();
        if (!string.IsNullOrEmpty(trangThai)) q = q.Where(d => d.TrangThai == trangThai);
        ViewBag.TrangThai = trangThai;
        ViewBag.ChoDuyet = await _db.DatBans.CountAsync(d => d.TrangThai == TrangThaiDatBan.ChoDuyet);
        return View(await q.OrderByDescending(d => d.NgayThanhToan ?? d.NgayTao).ToListAsync());
    }

    // Trang admin gọi định kỳ để biết có đơn mới cần duyệt hay không
    [HttpGet]
    public async Task<IActionResult> DemChoDuyet()
        => Ok(new { n = await _db.DatBans.CountAsync(d => d.TrangThai == TrangThaiDatBan.ChoDuyet) });

    // Phiếu đặt chỗ đầy đủ của một đơn (giống phiếu khách nhận được, kèm chi tiết hóa đơn và thao tác duyệt/hủy)
    [HttpGet]
    public async Task<IActionResult> Phieu(int id)
    {
        var b = await _db.DatBans.AsNoTracking().Include(d => d.KhuVuc).Include(d => d.Mons).Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (b == null) return NotFound();
        return View(b);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Duyet(int id, bool phieu = false) => Xong(await _svc.DuyetAsync(id), "Đã xác nhận đơn đặt bàn.", id, phieu);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> HoanThanh(int id, bool phieu = false) => Xong(await _svc.HoanThanhAsync(id), "Đã đánh dấu hoàn thành.", id, phieu);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Huy(int id, string? lyDo, bool phieu = false) => Xong(await _svc.HuyAsync(id, null, true, lyDo), "Đã hủy đơn đặt bàn.", id, phieu);

    private IActionResult Xong(string? loi, string ok, int id, bool phieu)
    {
        if (loi == null) TempData["Msg"] = ok; else TempData["Err"] = loi;
        return phieu ? RedirectToAction(nameof(Phieu), new { id }) : RedirectToAction(nameof(Reservations));
    }
}
