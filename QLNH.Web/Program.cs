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
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromMinutes(60);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<OrderService>();

builder.Services.AddDbContext<AuthDbContext>(o => o.UseSqlServer(
    builder.Configuration.GetConnectionString("Default"),
    s => s.MigrationsHistoryTable("__EFMigrationsHistory_Auth")));

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

builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(1));

builder.Services.AddScoped<IMailSender, SmtpMailSender>();
builder.Services.AddScoped<DatBanService>();
builder.Services.AddHostedService<TuHuyDatBanWorker>();

var app = builder.Build();
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Home/Error");

using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var appDb = sp.GetRequiredService<AppDbContext>();
    appDb.EnsureSchemaColumns();

    var auth = sp.GetRequiredService<AuthDbContext>();
    auth.Database.Migrate();

    var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var r in new[] { AccountController.RoleKhach, AccountController.RoleAdmin })
    {
        if (!await roles.RoleExistsAsync(r))
            await roles.CreateAsync(new IdentityRole(r));
    }

    await DbSeeder.SeedAsync(
        auth,
        sp.GetRequiredService<UserManager<AppUser>>(),
        app.Configuration,
        sp.GetRequiredService<DatBanService>());
}

app.Use(async (ctx, next) =>
{
    var p = ctx.Request.Path;
    if (HttpMethods.IsGet(ctx.Request.Method))
    {
        if (p.Equals("/Reservation", StringComparison.OrdinalIgnoreCase) ||
            p.Equals("/Reservation/Create", StringComparison.OrdinalIgnoreCase) ||
            p.Equals("/DatBan", StringComparison.OrdinalIgnoreCase) ||
            p.Equals("/DatBan/Index", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Response.Redirect("/Cart");
            return;
        }

        if (p.StartsWithSegments("/DatBan", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Response.Redirect("/");
            return;
        }
    }
    await next();
});

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
