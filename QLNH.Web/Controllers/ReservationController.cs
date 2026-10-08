using Microsoft.AspNetCore.Mvc;
using QLNH.Data;
using QLNH.Data.Entities;

namespace QLNH.Web.Controllers;

public class ReservationController : Controller
{
    private readonly AppDbContext _db;
    public ReservationController(AppDbContext db) => _db = db;

    [HttpGet] public IActionResult Create() => View(new DatBan());

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create([Bind("TenKhach,SoDienThoai,NgayGio,SoNguoi,GhiChu")] DatBan model)
    {
        if (model.NgayGio < DateTime.Now) ModelState.AddModelError(nameof(model.NgayGio), "Thời gian đặt bàn phải ở tương lai.");
        if (!ModelState.IsValid) return View(model);
        model.TrangThai = "Chờ xác nhận";
        _db.DatBans.Add(model);
        _db.SaveChanges();
        TempData["Msg"] = "Đặt bàn thành công! Nhà hàng sẽ liên hệ xác nhận qua số điện thoại của bạn.";
        return RedirectToAction(nameof(Create));
    }
}
