using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNH_Web.Data;
using QLNH_Web.Models;
using QLNH_Web.Services;
using QLNH_Web.ViewModels;

namespace QLNH_Web.Controllers;

// Trang đặt bàn theo giao diện Aura: /DatBan
public class DatBanController : Controller
{
    private readonly AuthDbContext _db;
    private readonly UserManager<AppUser> _users;
    private readonly IConfiguration _cfg;

    public DatBanController(AuthDbContext db, UserManager<AppUser> users, IConfiguration cfg) { _db = db; _users = users; _cfg = cfg; }

    // Tài khoản nhận cọc: đặt trong appsettings.json mục "Payment:Banks"; không có thì dùng tài khoản mẫu bên dưới
    private List<BankInfo> LayNganHang()
    {
        var ds = _cfg.GetSection("Payment:Banks").Get<List<BankInfo>>();
        if (ds != null && ds.Count > 0) return ds;
        return new List<BankInfo>
        {
            new() { Code = "VCB", Ten = "Vietcombank (VCB)", TenDayDu = "Ngân Hàng Vietcombank (VCB)", SoTaiKhoan = "9999-8888-AURA", ChuTaiKhoan = "NHÀ HÀNG AURA FINE DINING" },
            new() { Code = "MB", Ten = "MB Bank", TenDayDu = "Ngân Hàng Quân Đội (MB Bank)", SoTaiKhoan = "9999-6666-AURA", ChuTaiKhoan = "NHÀ HÀNG AURA FINE DINING" }
        };
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var vm = new DatBanPageVM
        {
            KhuVucs = await _db.KhuVucs.AsNoTracking().Where(k => k.HoatDong).OrderBy(k => k.Id).ToListAsync(),
            Mons = await _db.MucThucDons.AsNoTracking().Where(m => m.ConPhucVu).OrderBy(m => m.ThuTu).ThenBy(m => m.Id).ToListAsync(),
            Slots = DatBanService.Slots.ToList(),
            Banks = LayNganHang()
        };
        if (User.Identity?.IsAuthenticated == true)
        {
            var u = await _users.GetUserAsync(User);
            if (u != null)
            {
                vm.DaDangNhap = true;
                vm.HoTen = u.HoTen;
                vm.SoDienThoai = u.PhoneNumber ?? "";
                vm.Email = u.Email ?? "";
            }
        }
        return View(vm);
    }

    [HttpGet, Authorize]
    public async Task<IActionResult> LichSu()
    {
        var uid = _users.GetUserId(User);
        var ds = await _db.DatBans.AsNoTracking().Include(d => d.KhuVuc)
            .Where(d => d.UserId == uid && d.TrangThai != TrangThaiDatBan.ChoCoc).OrderByDescending(d => d.NgayGio).ToListAsync();
        return View(ds);
    }

    [HttpGet, Authorize]
    public async Task<IActionResult> Ve(string id)
    {
        var b = await _db.DatBans.AsNoTracking().Include(d => d.KhuVuc).Include(d => d.Mons)
            .FirstOrDefaultAsync(d => d.MaDatBan == id);
        if (b == null) return NotFound();
        if (b.UserId != _users.GetUserId(User) && !User.IsInRole(AccountController.RoleAdmin)) return Forbid();
        return View(b);
    }
}
