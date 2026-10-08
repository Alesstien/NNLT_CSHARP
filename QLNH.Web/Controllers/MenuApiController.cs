using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNH.Data;

namespace QLNH.Web.Controllers;

/// <summary>Web API: GET /api/mon-an, GET /api/mon-an/{id}, GET /api/danh-muc, GET /api/ban</summary>
[ApiController]
public class MenuApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public MenuApiController(AppDbContext db) => _db = db;

    [HttpGet("api/mon-an")]
    public IActionResult DanhSach([FromQuery] string? q, [FromQuery] int? danhMucId)
    {
        var query = _db.MonAns.Include(m => m.DanhMuc).Where(m => m.ConPhucVu);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(m => m.TenMon.Contains(q));
        if (danhMucId != null) query = query.Where(m => m.DanhMucId == danhMucId);
        return Ok(query.Select(m => new { m.Id, m.TenMon, m.Gia, DanhMuc = m.DanhMuc!.TenDanhMuc }).ToList());
    }

    [HttpGet("api/mon-an/{id:int}")]
    public IActionResult ChiTiet(int id)
    {
        var m = _db.MonAns.Include(x => x.DanhMuc).FirstOrDefault(x => x.Id == id);
        return m == null ? NotFound() : Ok(new { m.Id, m.TenMon, m.MoTa, m.Gia, DanhMuc = m.DanhMuc!.TenDanhMuc, m.ConPhucVu });
    }

    [HttpGet("api/danh-muc")]
    public IActionResult DanhMuc() => Ok(_db.DanhMucs.Select(d => new { d.Id, d.TenDanhMuc, d.MoTa }).ToList());

    [HttpGet("api/ban")]
    public IActionResult Ban() => Ok(_db.Bans.Select(b => new { b.Id, b.TenBan, b.SoGhe, b.TrangThai }).ToList());
}
