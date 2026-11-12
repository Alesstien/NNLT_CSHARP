using System.ComponentModel.DataAnnotations;
using QLNH_Web.Models;

namespace QLNH_Web.ViewModels;

public record SlotOption(string Value, string Label);
public record KhaDungItem(int KhuVucId, int ConLai, bool DuCho);
public record BaoGia(decimal TongMon, decimal PhuThu, decimal Vat, decimal PhiPhucVu, decimal GiamGia, decimal TongHoaDon);

public class BankInfo
{
    public string Code { get; set; } = "";
    public string Ten { get; set; } = "";
    public string TenDayDu { get; set; } = "";
    public string SoTaiKhoan { get; set; } = "";
    public string ChuTaiKhoan { get; set; } = "";
}

public class MonChonDto
{
    public int MucThucDonId { get; set; }
    [Range(1, 50)] public int SoLuong { get; set; }
}

public class DatBanRequest
{
    public int? Id { get; set; }
    [Required, StringLength(100)] public string HoTen { get; set; } = "";
    [Required, RegularExpression(@"^(0|\+84)\d{9}$", ErrorMessage = "Số điện thoại không hợp lệ")] public string SoDienThoai { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Ngay { get; set; } = "";
    [Required] public string Gio { get; set; } = "";
    [Range(1, 20)] public int SoKhach { get; set; }
    public int KhuVucId { get; set; }
    [StringLength(100)] public string? DipDacBiet { get; set; }
    [StringLength(500)] public string? GhiChu { get; set; }
    [StringLength(30)] public string? MaUuDai { get; set; }
    public string CheDoCoc { get; set; } = CheDo.CoDinh;
    public List<MonChonDto> Mons { get; set; } = new();
}

public class ThanhToanRequest
{
    [Required] public string PhuongThuc { get; set; } = "";
    public string? NganHang { get; set; }
    [Required] public string CheDoCoc { get; set; } = CheDo.CoDinh;
}

public class HuyRequest
{
    [StringLength(300)] public string? LyDo { get; set; }
}

public class DatBanPageVM
{
    public List<KhuVuc> KhuVucs { get; set; } = new();
    public List<MucThucDon> Mons { get; set; } = new();
    public List<SlotOption> Slots { get; set; } = new();
    public List<BankInfo> Banks { get; set; } = new();
    public bool DaDangNhap { get; set; }
    public string HoTen { get; set; } = "";
    public string SoDienThoai { get; set; } = "";
    public string Email { get; set; } = "";
}

public class AccountRow
{
    public string Id { get; set; } = "";
    public string HoTen { get; set; } = "";
    public string Email { get; set; } = "";
    public string SoDienThoai { get; set; } = "";
    public DateTime NgayTao { get; set; }
    public DateTime? LanDangNhapCuoi { get; set; }
    public string TrangThai { get; set; } = "";
    public bool DaKhoa { get; set; }
    public bool LaAdmin { get; set; }
    public int SoLanDatBan { get; set; }
}
