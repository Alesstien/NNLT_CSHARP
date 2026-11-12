using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNH.Data;

namespace QLNH.Web.Controllers;

[ApiController]
public class MenuApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public MenuApiController(AppDbContext db) => _db = db;

    [HttpGet("api/mon-an")]
    public IActionResult DanhSach([FromQuery] string? q, [FromQuery] int? danhMucId)
    {
        var query = _db.MonAns.AsNoTracking().Include(m => m.DanhMuc).Where(m => m.ConPhucVu);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var kw = q.Trim();
            query = query.Where(m => m.TenMon.Contains(kw) || (m.MoTa != null && m.MoTa.Contains(kw)));
        }
        if (danhMucId != null) query = query.Where(m => m.DanhMucId == danhMucId);
        return Ok(query.OrderBy(m => m.DanhMucId).ThenBy(m => m.Id)
            .Select(m => new { m.Id, m.TenMon, m.MoTa, m.Gia, m.DanhMucId, DanhMuc = m.DanhMuc!.TenDanhMuc, m.HinhAnh, m.ConPhucVu })
            .ToList());
    }

    [HttpGet("api/mon-an/{id:int}")]
    public IActionResult ChiTiet(int id)
    {
        var m = _db.MonAns.AsNoTracking().Include(x => x.DanhMuc).FirstOrDefault(x => x.Id == id);
        return m == null
            ? NotFound(new { status = 404, message = $"Không tìm thấy món ăn có ID = {id}." })
            : Ok(new { m.Id, m.TenMon, m.MoTa, m.Gia, m.DanhMucId, DanhMuc = m.DanhMuc!.TenDanhMuc, m.HinhAnh, m.ConPhucVu });
    }

    [HttpGet("api/danh-muc")]
    public IActionResult DanhMuc() => Ok(_db.DanhMucs.Select(d => new { d.Id, d.TenDanhMuc, d.MoTa }).ToList());

    [HttpGet("api/ban")]
    public IActionResult Ban() => Ok(_db.Bans.Select(b => new { b.Id, b.TenBan, b.SoGhe, b.TrangThai }).ToList());
}
