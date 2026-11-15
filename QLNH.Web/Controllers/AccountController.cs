using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNH_Web.Models;
using QLNH_Web.Services;
using QLNH_Web.ViewModels;

namespace QLNH_Web.Controllers;

public class AccountController : Controller
{
    public const string RoleKhach = "KhachHang";
    public const string RoleAdmin = "Admin";
    private readonly UserManager<AppUser> _users;
    private readonly SignInManager<AppUser> _signIn;
    private readonly DatBanService _svc;

    public AccountController(UserManager<AppUser> users, SignInManager<AppUser> signIn, DatBanService svc)
    {
        _users = users;
        _signIn = signIn;
        _svc = svc;
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Register()
        => User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Home") : View(new RegisterVM());

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVM m)
    {
        if (!ModelState.IsValid) return View(m);

        var sdt = ChuanHoaSdt(m.SoDienThoai);
        if (await _users.Users.AnyAsync(u => u.PhoneNumber == sdt))
        {
            ModelState.AddModelError(nameof(m.SoDienThoai), "Sá»‘ Ä‘iá»‡n thoáº¡i Ä‘Ã£ Ä‘Æ°á»£c Ä‘Äƒng kÃ½");
            return View(m);
        }

        var user = new AppUser
        {
            UserName = m.Email.Trim(),
            Email = m.Email.Trim(),
            PhoneNumber = sdt,
            HoTen = m.HoTen.Trim(),
            LanDangNhapCuoi = DateTime.Now
        };

        var result = await _users.CreateAsync(user, m.MatKhau);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError("", DichLoi(e));
            return View(m);
        }

        await _users.AddToRoleAsync(user, RoleKhach);
        await _svc.DongBoTaiKhoanSangWinFormsAsync(user, m.MatKhau, true, false);
        await _signIn.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
        => User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Home") : View(new LoginVM { ReturnUrl = returnUrl });

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVM m)
    {
        if (!ModelState.IsValid) return View(m);

        var key = m.TaiKhoan.Trim();
        AppUser? user = key.Contains('@')
            ? await _users.FindByEmailAsync(key)
            : await _users.Users.FirstOrDefaultAsync(u => u.PhoneNumber == ChuanHoaSdt(key));

        if (user != null)
        {
            var r = await _signIn.PasswordSignInAsync(user, m.MatKhau, m.NhoDangNhap, lockoutOnFailure: true);
            if (r.Succeeded)
            {
                user.LanDangNhapCuoi = DateTime.Now;
                await _users.UpdateAsync(user);
                return !string.IsNullOrEmpty(m.ReturnUrl) && Url.IsLocalUrl(m.ReturnUrl)
                    ? LocalRedirect(m.ReturnUrl)
                    : RedirectToAction("Index", "Home");
            }
            if (r.IsLockedOut)
            {
                var end = await _users.GetLockoutEndDateAsync(user);
                ModelState.AddModelError("", end > DateTimeOffset.UtcNow.AddYears(50)
                    ? "TÃ i khoáº£n Ä‘Ã£ bá»‹ quáº£n trá»‹ viÃªn khÃ³a. Vui lÃ²ng liÃªn há»‡ nhÃ  hÃ ng."
                    : "TÃ i khoáº£n táº¡m khÃ³a do nháº­p sai máº­t kháº©u nhiá»u láº§n. Vui lÃ²ng thá»­ láº¡i sau 5 phÃºt.");
                return View(m);
            }
        }

        ModelState.AddModelError("", "TÃ i khoáº£n hoáº·c máº­t kháº©u khÃ´ng Ä‘Ãºng");
        return View(m);
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet, AllowAnonymous]
    public IActionResult AccessDenied() => Content("Báº¡n khÃ´ng cÃ³ quyá»n truy cáº­p trang nÃ y.");

    private static string ChuanHoaSdt(string s)
    {
        s = s.Trim().Replace(" ", "").Replace("-", "");
        return s.StartsWith("+84") ? "0" + s[3..] : s;
    }

    private static string DichLoi(IdentityError e) => e.Code switch
    {
        "DuplicateUserName" or "DuplicateEmail" => "Email Ä‘Ã£ Ä‘Æ°á»£c Ä‘Äƒng kÃ½",
        "PasswordTooShort" => "Máº­t kháº©u quÃ¡ ngáº¯n",
        "PasswordRequiresDigit" => "Máº­t kháº©u cáº§n Ã­t nháº¥t má»™t chá»¯ sá»‘",
        _ => e.Description
    };
}

