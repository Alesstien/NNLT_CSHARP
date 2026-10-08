using System.Text.Json;

namespace QLNH.Web.Models;

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

public class CheckoutViewModel
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng nhập họ tên")]
    public string TenKhach { get; set; } = "";
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [System.ComponentModel.DataAnnotations.RegularExpression(@"^(0|\+84)\d{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ")]
    public string SoDienThoai { get; set; } = "";
    public string? GhiChu { get; set; }
}
