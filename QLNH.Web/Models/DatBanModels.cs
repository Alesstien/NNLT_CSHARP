namespace QLNH_Web.Models;

public static class TrangThaiDatBan
{
    public const string ChoCoc = "Chờ đặt cọc";
    public const string ChoDuyet = "Đang chờ duyệt";
    public const string DaXacNhan = "Đã xác nhận";
    public const string DaHuy = "Đã hủy";
    public const string HoanThanh = "Đã hoàn thành";
    public static readonly string[] TatCa = { ChoCoc, ChoDuyet, DaXacNhan, DaHuy, HoanThanh };
}

public static class CheDo
{
    public const string CoDinh = "CocCoDinh";
    public const string Ba30 = "Coc30";
    public const string Tra100 = "Tra100";
    public static readonly string[] TatCa = { CoDinh, Ba30, Tra100 };
}

public class KhuVuc
{
    public int Id { get; set; }
    public string Ten { get; set; } = "";
    public string MoTa { get; set; } = "";
    public decimal PhuThu { get; set; }
    public int SucChua { get; set; }
    public bool HoatDong { get; set; } = true;
    public bool ChoTiec { get; set; }
}

public class MucThucDon
{
    public int Id { get; set; }
    public string Ten { get; set; } = "";
    public string MoTa { get; set; } = "";
    public decimal Gia { get; set; }
    public string DanhMuc { get; set; } = "";
    public string? HinhAnh { get; set; }
    public string Emoji { get; set; } = "";
    public int ThuTu { get; set; }
    public bool ConPhucVu { get; set; } = true;
}

public class UuDai
{
    public int Id { get; set; }
    public string Ma { get; set; } = "";
    public string MoTa { get; set; } = "";
    public int PhanTramGiam { get; set; }
    public decimal? GiamToiDa { get; set; }
    public DateTime? HanDung { get; set; }
    public bool HoatDong { get; set; } = true;
}

public class DatBan
{
    public int Id { get; set; }
    public string MaDatBan { get; set; } = "";
    public string? UserId { get; set; }
    public AppUser? User { get; set; }

    public string HoTen { get; set; } = "";
    public string SoDienThoai { get; set; } = "";
    public string Email { get; set; } = "";

    public DateTime NgayGio { get; set; }
    public int SoKhach { get; set; }
    public int SoBan { get; set; } = 1;
    public int KhuVucId { get; set; }
    public KhuVuc? KhuVuc { get; set; }
    public string? DipDacBiet { get; set; }
    public string? GhiChu { get; set; }

    public decimal TongMon { get; set; }
    public decimal PhuThuKhuVuc { get; set; }
    public decimal Vat { get; set; }
    public decimal PhiPhucVu { get; set; }
    public string? MaUuDai { get; set; }
    public decimal GiamGia { get; set; }
    public decimal TongHoaDon { get; set; }

    public string CheDoCoc { get; set; } = CheDo.CoDinh;
    public decimal TienCoc { get; set; }
    public decimal ConLai { get; set; }
    public string PhuongThucThanhToan { get; set; } = "";
    public string? NganHang { get; set; }
    public string MaGiaoDich { get; set; } = "";
    public bool DaThanhToanCoc { get; set; }
    public DateTime? NgayThanhToan { get; set; }
    public DateTime? GiuChoDen { get; set; }

    public string TrangThai { get; set; } = TrangThaiDatBan.ChoCoc;
    public DateTime NgayTao { get; set; } = DateTime.Now;
    public DateTime? NgayCapNhat { get; set; }
    public string? LyDoHuy { get; set; }

    public List<DatBanMon> Mons { get; set; } = new();
}

public class DatBanMon
{
    public int Id { get; set; }
    public int DatBanId { get; set; }
    public DatBan? DatBan { get; set; }
    public int? MucThucDonId { get; set; }
    public MucThucDon? Mon { get; set; }
    public int? MonAnId { get; set; }    public string TenMon { get; set; } = "";
    public decimal DonGia { get; set; }
    public int SoLuong { get; set; }
}
