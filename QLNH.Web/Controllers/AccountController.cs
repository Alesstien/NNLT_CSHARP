using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNH_Web.Models;
using QLNH_Web.ViewModels;

namespace QLNH_Web.Controllers;

public class AccountController : Controller
{
    public const string RoleKhach = "KhachHang";
    public const string RoleAdmin = "Admin";
    private readonly UserManager<AppUser> _users;
    private readonly SignInManager<AppUser> _signIn;

    public AccountController(UserManager<AppUser> users, SignInManager<AppUser> signIn)
    {
        _users = users;
        _signIn = signIn;
    }

    // ---------- Đăng ký ----------
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
            ModelState.AddModelError(nameof(m.SoDienThoai), "Số điện thoại đã được đăng ký");
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
        await _signIn.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Index", "Home");
    }

    // ---------- Đăng nhập ----------
    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
        => User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Home") : View(new LoginVM { ReturnUrl = returnUrl });

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVM m)
    {
        if (!ModelState.IsValid) return View(m);

        // Cho phép đăng nhập bằng email hoặc số điện thoại
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
                    ? "Tài khoản đã bị quản trị viên khóa. Vui lòng liên hệ nhà hàng."
                    : "Tài khoản tạm khóa do nhập sai mật khẩu nhiều lần. Vui lòng thử lại sau 5 phút.");
                return View(m);
            }
        }

        ModelState.AddModelError("", "Tài khoản hoặc mật khẩu không đúng");
        return View(m);
    }

    // ---------- Đăng xuất ----------
    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet, AllowAnonymous]
    public IActionResult AccessDenied() => Content("Bạn không có quyền truy cập trang này.");

    // ---------- Helpers ----------
    // 0912345678 và +84912345678 được coi là cùng một số
    private static string ChuanHoaSdt(string s)
    {
        s = s.Trim().Replace(" ", "").Replace("-", "");
        return s.StartsWith("+84") ? "0" + s[3..] : s;
    }

    private static string DichLoi(IdentityError e) => e.Code switch
    {
        "DuplicateUserName" or "DuplicateEmail" => "Email đã được đăng ký",
        "PasswordTooShort" => "Mật khẩu quá ngắn",
        "PasswordRequiresDigit" => "Mật khẩu cần ít nhất một chữ số",
        _ => e.Description
    };
}
