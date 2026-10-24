using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace QLNH_Web.Models;

/// <summary>Giỏ hàng lưu trong Session: MonAnId -> số lượng.</summary>
public static class CartHelper
{
    private const string Key = "cart";

    public static Dictionary<int, int> Get(ISession s)
    {
        var json = s.GetString(Key);
        return string.IsNullOrEmpty(json) ? new() : JsonSerializer.Deserialize<Dictionary<int, int>>(json) ?? new();
    }
    public static void Save(ISession s, Dictionary<int, int> cart) => s.SetString(Key, JsonSerializer.Serialize(cart));
    public static void Clear(ISession s) => s.Remove(Key);
}

public class CartLine
{
    public int MonAnId { get; set; }
    public string TenMon { get; set; } = "";
    public decimal DonGia { get; set; }
    public int SoLuong { get; set; }
    public decimal ThanhTien => DonGia * SoLuong;
}

/// <summary>Thông tin đặt bàn khách đang nhập ở trang giỏ hàng (lưu tạm trong Session).</summary>
public class BookingDraft
{
    private const string Key = "booking";
    public static readonly string[] CacLoaiLe = { "Ăn uống thường", "Đám cưới", "Đám hỏi", "Sinh nhật", "Hội nghị / Liên hoan" };

    public DateTime? ThoiGianDen { get; set; }
    public int SoNguoi { get; set; } = 2;
    public string LoaiLe { get; set; } = CacLoaiLe[0];
    public int KhuVucId { get; set; }
    public string? GhiChu { get; set; }

    public bool LaTiec => LoaiLe != CacLoaiLe[0];
    /// <summary>Tiệc: 10 khách / bàn (mâm), làm tròn lên. Ăn thường: 1.</summary>
    public int SoBan => LaTiec ? (int)Math.Ceiling(SoNguoi / 10.0) : 1;

    public static BookingDraft Get(ISession s)
    {
        var json = s.GetString(Key);
        return string.IsNullOrEmpty(json) ? new() : JsonSerializer.Deserialize<BookingDraft>(json) ?? new();
    }
    public void Save(ISession s) => s.SetString(Key, JsonSerializer.Serialize(this));
    public static void Clear(ISession s) => s.Remove(Key);
}

/// <summary>Kết quả tính tiền. Luôn tính ở server, không tin số tiền từ trình duyệt.</summary>
public class Bill
{
    public int SoBan { get; set; }
    public decimal TienMon { get; set; }
    public decimal PhuThu { get; set; }
    public decimal Giam { get; set; }
    public decimal Vat { get; set; }
    public decimal PhiPhucVu { get; set; }
    public decimal Tong { get; set; }
    public decimal Coc { get; set; }
    public decimal ConLai { get; set; }

    public static decimal TinhCoc(string cheDo, decimal tong) => cheDo switch
    {
        CheDo.CoDinh => Math.Min(500_000m, tong),
        CheDo.Ba30 => Math.Round(tong * 0.3m),
        _ => tong
    };

    public static Bill Tinh(decimal monMotMam, BookingDraft b, KhuVuc kv, UuDai? ud, string cheDo)
    {
        var r = new Bill { SoBan = b.SoBan };
        r.TienMon = monMotMam * r.SoBan;
        r.PhuThu = kv.PhuThu;
        if (ud != null)
            r.Giam = Math.Round(Math.Min(r.TienMon * ud.PhanTramGiam / 100m, ud.GiamToiDa ?? decimal.MaxValue));
        var coSo = r.TienMon - r.Giam;
        r.Vat = Math.Round((coSo + r.PhuThu) * 0.08m);
        r.PhiPhucVu = Math.Round(coSo * 0.05m);
        r.Tong = coSo + r.PhuThu + r.Vat + r.PhiPhucVu;
        r.Coc = TinhCoc(cheDo, r.Tong);
        r.ConLai = r.Tong - r.Coc;
        return r;
    }
}

public class CartPageVm
{
    public List<CartLine> Lines { get; set; } = new();
    public BookingDraft Booking { get; set; } = new();
    public List<KhuVuc> KhuVucs { get; set; } = new();
    public KhuVuc? KhuVuc { get; set; }
    public Bill? Bill { get; set; }
}

public class PaymentInput
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    public string HoTen { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [RegularExpression(@"^(0|\+84)\d{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ")]
    public string SoDienThoai { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = "";

    public string? Ma { get; set; }
    public string CheDoCoc { get; set; } = CheDo.CoDinh;
    public string PhuongThuc { get; set; } = "VIETQR";   // VIETQR | MOMO | VNPAY | THE
    public string NganHang { get; set; } = "VCB";        // VCB | MB (khi VietQR)
}

public class PaymentVm
{
    public PaymentInput Input { get; set; } = new();
    public List<CartLine> Lines { get; set; } = new();
    public BookingDraft Booking { get; set; } = new();
    public KhuVuc KhuVuc { get; set; } = null!;
    public Bill Bill { get; set; } = null!;
    public string? MaMsg { get; set; }
    public bool MaHopLe { get; set; }
    public string MaGiaoDich { get; set; } = "";
    public DateTime GiuChoDen { get; set; }
}
