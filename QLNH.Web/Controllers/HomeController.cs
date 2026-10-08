using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNH.Data;

namespace QLNH.Web.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    public HomeController(AppDbContext db) => _db = db;

    public IActionResult Index()
    {
        ViewBag.DanhMucs = _db.DanhMucs.Include(d => d.MonAns).OrderBy(d => d.TenDanhMuc).ToList();
        var noiBat = _db.MonAns.Include(m => m.DanhMuc).Where(m => m.ConPhucVu).OrderByDescending(m => m.Gia).Take(6).ToList();
        return View(noiBat);
    }

    public IActionResult Error() => View();
}
