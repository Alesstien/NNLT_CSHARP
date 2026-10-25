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
        ViewBag.DanhMucs = _db.DanhMucs.Include(d => d.MonAns).OrderBy(d => d.Id).ToList();
        var tatCaMon = _db.MonAns.Include(m => m.DanhMuc).Where(m => m.ConPhucVu).OrderBy(m => m.DanhMucId).ThenBy(m => m.Id).ToList();
        return View(tatCaMon);
    }

    public IActionResult Error() => View();
}
