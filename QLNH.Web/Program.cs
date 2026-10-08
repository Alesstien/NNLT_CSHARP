using Microsoft.EntityFrameworkCore;
using QLNH.Data;
using QLNH.Data.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSession(o => { o.IdleTimeout = TimeSpan.FromMinutes(60); o.Cookie.HttpOnly = true; o.Cookie.IsEssential = true; });
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<OrderService>();   // Dependency Injection

var app = builder.Build();
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Home/Error");
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.MapControllers();   // Web API (attribute routing)
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
