using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QLNH.Data;
using QLNH.Data.Services;
using QLNH_Web.Controllers;
using QLNH_Web.Data;
using QLNH_Web.Models;
using QLNH_Web.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSession(o => { o.IdleTimeout = TimeSpan.FromMinutes(60); o.Cookie.HttpOnly = true; o.Cookie.IsEssential = true; });
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<OrderService>();   // Dependency Injection

// ---- Đăng nhập / đăng ký (ASP.NET Core Identity) ----
builder.Services.AddDbContext<AuthDbContext>(o => o.UseSqlServer(
    builder.Configuration.GetConnectionString("Default"),
    s => s.MigrationsHistoryTable("__EFMigrationsHistory_Auth")));   // tách lịch sử migration khỏi AppDbContext

builder.Services.AddIdentity<AppUser, IdentityRole>(o =>
{
    o.User.RequireUniqueEmail = true;
    o.Password.RequiredLength = 6;
    o.Password.RequireNonAlphanumeric = false;
    o.Password.RequireUppercase = false;
    o.Lockout.AllowedForNewUsers = true;
    o.Lockout.MaxFailedAccessAttempts = 5;
    o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
})
.AddEntityFrameworkStores<AuthDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/Account/AccessDenied";
    o.ExpireTimeSpan = TimeSpan.FromDays(14);
    o.SlidingExpiration = true;
});
// Kiểm tra lại tài khoản mỗi phút: admin khóa tài khoản thì phiên đang đăng nhập bị đăng xuất sau tối đa ~1 phút
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(1));

// ---- Đặt bàn ----
builder.Services.AddScoped<IMailSender, SmtpMailSender>();
builder.Services.AddScoped<DatBanService>();

var app = builder.Build();
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Home/Error");

// Tự áp migration, tạo role, dữ liệu mẫu (khu vực, thực đơn) và tài khoản admin khi khởi động
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var auth = sp.GetRequiredService<AuthDbContext>();
    auth.Database.Migrate();
    var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var r in new[] { AccountController.RoleKhach, AccountController.RoleAdmin })
        if (!await roles.RoleExistsAsync(r)) await roles.CreateAsync(new IdentityRole(r));
    await DbSeeder.SeedAsync(auth, sp.GetRequiredService<UserManager<AppUser>>(), app.Configuration, sp.GetRequiredService<DatBanService>());
}

// Đường dẫn cũ của trang đặt bàn (menu cũ trỏ tới /Reservation/Create) -> trang đặt bàn mới
app.Use(async (ctx, next) =>
{
    var p = ctx.Request.Path;
    if (HttpMethods.IsGet(ctx.Request.Method) &&
        (p.Equals("/Reservation", StringComparison.OrdinalIgnoreCase) || p.Equals("/Reservation/Create", StringComparison.OrdinalIgnoreCase)))
    { ctx.Response.Redirect("/DatBan"); return; }
    await next();
});

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();   // Web API (attribute routing)
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
