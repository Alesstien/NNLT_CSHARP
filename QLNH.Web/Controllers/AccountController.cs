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

    public AccountController(
        UserManager<AppUser> users,
        SignInManager<AppUser> signIn,
        DatBanService svc)
    {
        _users = users;
        _signIn = signIn;
        _svc = svc;
    }

    // Đăng ký - GET
    [HttpGet("/register"), AllowAnonymous]
    public IActionResult Register()
        => User.Identity?.IsAuthenticated == true
            ? RedirectToAction("Index", "Home")
            : View(new RegisterVM());

    // Đăng ký - POST
    [HttpPost("/register"), AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVM m)
    {
        if (!ModelState.IsValid)
            return View(m);

        var sdt = ChuanHoaSdt(m.SoDienThoai);

        if (await _users.Users.AnyAsync(u => u.PhoneNumber == sdt))
        {
            ModelState.AddModelError(
                nameof(m.SoDienThoai),
                "Số điện thoại đã được đăng ký");

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
            // Lỗi email/tên đăng nhập trùng được hiển thị ngay dưới ô Email.
            bool emailDaTonTai = result.Errors.Any(
                e => e.Code == "DuplicateUserName"
                  || e.Code == "DuplicateEmail");

            if (emailDaTonTai)
            {
                ModelState.AddModelError(
                    nameof(m.Email),
                    "Email đã được đăng ký");
            }

            // Những lỗi khác vẫn hiển thị tại khu vực thông báo chung.
            foreach (var e in result.Errors.Where(
                e => e.Code != "DuplicateUserName"
                  && e.Code != "DuplicateEmail"))
            {
                ModelState.AddModelError("", DichLoi(e));
            }

            return View(m);
        }

        await _users.AddToRoleAsync(user, RoleKhach);
        await _svc.DongBoTaiKhoanSangWinFormsAsync(user, m.MatKhau, true, false);
        await _signIn.SignInAsync(user, isPersistent: false);

        return RedirectToAction("Index", "Home");
    }

    // Đăng nhập - GET
    [HttpGet("/login"), AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
        => User.Identity?.IsAuthenticated == true
            ? RedirectToAction("Index", "Home")
            : View(new LoginVM { ReturnUrl = returnUrl });

    // Đăng nhập - POST
    [HttpPost("/login"), AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVM m)
    {
        if (!ModelState.IsValid)
            return View(m);

        var key = m.TaiKhoan.Trim();

        AppUser? user = key.Contains('@')
            ? await _users.FindByEmailAsync(key)
            : await _users.Users.FirstOrDefaultAsync(
                u => u.PhoneNumber == ChuanHoaSdt(key));

        if (user != null)
        {
            var r = await _signIn.PasswordSignInAsync(
                user,
                m.MatKhau,
                m.NhoDangNhap,
                lockoutOnFailure: true);

            if (r.Succeeded)
            {
                user.LanDangNhapCuoi = DateTime.Now;
                await _users.UpdateAsync(user);

                return !string.IsNullOrEmpty(m.ReturnUrl)
                    && Url.IsLocalUrl(m.ReturnUrl)
                    ? LocalRedirect(m.ReturnUrl)
                    : RedirectToAction("Index", "Home");
            }

            if (r.IsLockedOut)
            {
                var end = await _users.GetLockoutEndDateAsync(user);

                ModelState.AddModelError(
                    "",
                    end > DateTimeOffset.UtcNow.AddYears(50)
                        ? "Tài khoản đã bị quản trị viên khóa. Vui lòng liên hệ nhà hàng."
                        : "Tài khoản tạm khóa do nhập sai mật khẩu nhiều lần. Vui lòng thử lại sau 5 phút.");

                return View(m);
            }
        }

        ModelState.AddModelError(
            "",
            "Tài khoản hoặc mật khẩu không đúng");

        return View(m);
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet, AllowAnonymous]
    public IActionResult AccessDenied()
        => Content("Bạn không có quyền truy cập trang này.");

    private static string ChuanHoaSdt(string s)
    {
        s = s.Trim().Replace(" ", "").Replace("-", "");
        return s.StartsWith("+84") ? "0" + s[3..] : s;
    }

    private static string DichLoi(IdentityError e) => e.Code switch
    {
        "DuplicateUserName" or "DuplicateEmail"
            => "Email đã được đăng ký",

        "PasswordTooShort"
            => "Mật khẩu quá ngắn",

        "PasswordRequiresDigit"
            => "Mật khẩu cần ít nhất một chữ số",

        _ => e.Description
    };
}
