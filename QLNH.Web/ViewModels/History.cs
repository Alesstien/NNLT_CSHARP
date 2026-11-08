using QLNH_Web.Models;

namespace QLNH_Web.ViewModels;

public class LichSuDatBanVm
{
    public List<DatBan> DonHangs { get; set; } = new();

    public DateTime? TuNgay { get; set; }
    public DateTime? DenNgay { get; set; }
    public string? TrangThai { get; set; }

    public int Trang { get; set; } = 1;
    public int TongTrang { get; set; }
    public int TongDon { get; set; }
}
