using Microsoft.AspNetCore.Mvc;
using QLNH.Data;
using QLNH.Data.Services;
using QLNH.Web.Models;

namespace QLNH.Web.Controllers;

public class CartController : Controller
{
    private readonly AppDbContext _db;
    private readonly OrderService _orders;
    public CartController(AppDbContext db, OrderService orders) { _db = db; _orders = orders; }

    private List<CartLine> BuildLines()
    {
        var cart = CartHelper.Get(HttpContext.Session);
        var ids = cart.Keys.ToList();
        return _db.MonAns.Where(m => ids.Contains(m.Id))
            .AsEnumerable()
            .Select(m => new CartLine { MonAnId = m.Id, TenMon = m.TenMon, DonGia = m.Gia, SoLuong = cart[m.Id] }).ToList();
    }

    public IActionResult Index() => View(BuildLines());

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Add(int id, int soLuong = 1)
    {
        if (soLuong < 1) soLuong = 1;
        if (!_db.MonAns.Any(m => m.Id == id && m.ConPhucVu)) return NotFound();
        var cart = CartHelper.Get(HttpContext.Session);
        cart[id] = cart.GetValueOrDefault(id) + soLuong;
        CartHelper.Save(HttpContext.Session, cart);
        TempData["Msg"] = "Đã thêm món vào giỏ hàng.";
        return Redirect(Request.Headers.Referer.ToString() is { Length: > 0 } r ? r : "/Menu");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Update(int id, int soLuong)
    {
        var cart = CartHelper.Get(HttpContext.Session);
        if (soLuong <= 0) cart.Remove(id); else cart[id] = soLuong;
        CartHelper.Save(HttpContext.Session, cart);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Remove(int id) => Update(id, 0);

    [HttpGet]
    public IActionResult Checkout()
    {
        if (BuildLines().Count == 0) return RedirectToAction(nameof(Index));
        ViewBag.Lines = BuildLines();
        return View(new CheckoutViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Checkout(CheckoutViewModel vm)
    {
        var lines = BuildLines();
        if (lines.Count == 0) return RedirectToAction(nameof(Index));
        ViewBag.Lines = lines;
        if (!ModelState.IsValid) return View(vm);
        try
        {
            var don = _orders.TaoDon(vm.TenKhach, vm.SoDienThoai, null, vm.GhiChu, lines.Select(l => new DongDat(l.MonAnId, l.SoLuong)));
            CartHelper.Clear(HttpContext.Session);
            return RedirectToAction(nameof(Success), new { id = don.Id });
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            ModelState.AddModelError("", ex.Message);
            return View(vm);
        }
    }

    public IActionResult Success(int id) => View(model: id);
}
