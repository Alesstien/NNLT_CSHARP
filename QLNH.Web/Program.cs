using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QLNH.Data;
using QLNH.Data.Services;
using QLNH_Web.Controllers;
using QLNH_Web.Data;
using QLNH_Web.Models;

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

var app = builder.Build();
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Home/Error");

// Tự áp migration + tạo role KhachHang khi khởi động
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.Migrate();
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    if (!await roles.RoleExistsAsync(AccountController.RoleKhach))
        await roles.CreateAsync(new IdentityRole(AccountController.RoleKhach));
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();   // Web API (attribute routing)
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
