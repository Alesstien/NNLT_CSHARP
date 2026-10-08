using System.ComponentModel.DataAnnotations;

namespace QLNH.Data.Entities;

public static class TrangThaiDon
{
    public const string Moi = "Mới";
    public const string DangCheBien = "Đang chế biến";
    public const string HoanThanh = "Hoàn thành";
    public const string DaThanhToan = "Đã thanh toán";
    public const string DaHuy = "Đã hủy";
    public static readonly string[] TatCa = { Moi, DangCheBien, HoanThanh, DaThanhToan, DaHuy };
}

public class VaiTro
{
    public int Id { get; set; }
    [Required, StringLength(50)] public string TenVaiTro { get; set; } = "";
    public List<TaiKhoan> TaiKhoans { get; set; } = new();
}

public class TaiKhoan
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Tên đăng nhập không được để trống"), StringLength(50)]
    public string TenDangNhap { get; set; } = "";
    [Required, StringLength(200)] public string MatKhauHash { get; set; } = "";
    [Required(ErrorMessage = "Họ tên không được để trống"), StringLength(100)]
    public string HoTen { get; set; } = "";
    public int VaiTroId { get; set; } = 2;
    public VaiTro? VaiTro { get; set; }
    public bool HoatDong { get; set; } = true;
}

public class DanhMuc
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Tên danh mục không được để trống"), StringLength(100)]
    public string TenDanhMuc { get; set; } = "";
    [StringLength(300)] public string? MoTa { get; set; }
    public List<MonAn> MonAns { get; set; } = new();
}

public class MonAn
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Tên món không được để trống"), StringLength(150)]
    public string TenMon { get; set; } = "";
    [StringLength(500)] public string? MoTa { get; set; }
    [Range(0, 100000000, ErrorMessage = "Giá phải từ 0 trở lên")]
    public decimal Gia { get; set; }
    [StringLength(300)] public string? HinhAnh { get; set; }
    public int DanhMucId { get; set; }
    public DanhMuc? DanhMuc { get; set; }
    public bool ConPhucVu { get; set; } = true;
}

public class Ban
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Tên bàn không được để trống"), StringLength(50)]
    public string TenBan { get; set; } = "";
    [Range(1, 50, ErrorMessage = "Số ghế từ 1 đến 50")]
    public int SoGhe { get; set; } = 2;
    [Required, StringLength(30)] public string TrangThai { get; set; } = "Trống";
}

public class DonHang
{
    public int Id { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.Now;
    [Required(ErrorMessage = "Vui lòng nhập tên khách"), StringLength(100)]
    public string TenKhach { get; set; } = "";
    [StringLength(20)] public string? SoDienThoai { get; set; }
    public int? BanId { get; set; }
    public Ban? Ban { get; set; }
    [Required, StringLength(30)] public string TrangThai { get; set; } = TrangThaiDon.Moi;
    public decimal TongTien { get; set; }
    [StringLength(300)] public string? GhiChu { get; set; }
    public List<ChiTietDonHang> ChiTiets { get; set; } = new();
}

public class ChiTietDonHang
{
    public int Id { get; set; }
    public int DonHangId { get; set; }
    public DonHang? DonHang { get; set; }
    public int MonAnId { get; set; }
    public MonAn? MonAn { get; set; }
    [Range(1, 1000)] public int SoLuong { get; set; } = 1;
    public decimal DonGia { get; set; }
    public decimal ThanhTien => SoLuong * DonGia;
}

public class DatBan
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập họ tên"), StringLength(100)]
    public string TenKhach { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại"), StringLength(20)]
    [RegularExpression(@"^(0|\+84)\d{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ")]
    public string SoDienThoai { get; set; } = "";
    public DateTime NgayGio { get; set; } = DateTime.Now.AddHours(2);
    [Range(1, 50, ErrorMessage = "Số người từ 1 đến 50")]
    public int SoNguoi { get; set; } = 2;
    public int? BanId { get; set; }
    public Ban? Ban { get; set; }
    [Required, StringLength(30)] public string TrangThai { get; set; } = "Chờ xác nhận";
    [StringLength(300)] public string? GhiChu { get; set; }
}
