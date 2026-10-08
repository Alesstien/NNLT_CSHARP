using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNH.Data;

namespace QLNH.Web.Controllers;

public class MenuController : Controller
{
    private readonly AppDbContext _db;
    public MenuController(AppDbContext db) => _db = db;

    // Danh mục + tìm kiếm + lọc theo giá
    public IActionResult Index(string? q, int? danhMucId, decimal? giaTu, decimal? giaDen)
    {
        var query = _db.MonAns.Include(m => m.DanhMuc).Where(m => m.ConPhucVu);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(m => m.TenMon.Contains(q) || (m.MoTa != null && m.MoTa.Contains(q)));
        if (danhMucId != null) query = query.Where(m => m.DanhMucId == danhMucId);
        if (giaTu != null) query = query.Where(m => m.Gia >= giaTu);
        if (giaDen != null) query = query.Where(m => m.Gia <= giaDen);

        ViewBag.DanhMucs = _db.DanhMucs.OrderBy(d => d.TenDanhMuc).ToList();
        ViewBag.Q = q; ViewBag.DanhMucId = danhMucId; ViewBag.GiaTu = giaTu; ViewBag.GiaDen = giaDen;
        return View(query.OrderBy(m => m.DanhMuc!.TenDanhMuc).ThenBy(m => m.TenMon).ToList());
    }

    public IActionResult Details(int id)
    {
        var mon = _db.MonAns.Include(m => m.DanhMuc).FirstOrDefault(m => m.Id == id);
        if (mon == null) return NotFound();
        ViewBag.LienQuan = _db.MonAns.Where(m => m.DanhMucId == mon.DanhMucId && m.Id != id && m.ConPhucVu).Take(3).ToList();
        return View(mon);
    }
}
