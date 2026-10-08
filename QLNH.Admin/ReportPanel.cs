using System.Text;
using QLNH.Data.Services;

namespace QLNH.Admin;
///ReportPanel.cs: Giao diện báo cáo thống kê (doanh thu, số lượng đơn, các món bán chạy).
/// <summary>Thống kê doanh thu + món bán chạy, xuất CSV (chức năng mở rộng).</summary>
public class ReportPanel : UserControl
{
    private readonly DateTimePicker dtTu = new() { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30) };
    private readonly DateTimePicker dtDen = new() { Format = DateTimePickerFormat.Short, Value = DateTime.Today };
    private readonly DataGridView gridNgay = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, BackgroundColor = Color.White };
    private readonly DataGridView gridMon = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, BackgroundColor = Color.White };
    private readonly Label lblTong = new() { AutoSize = true, Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.Firebrick, Padding = new Padding(10, 6, 0, 0) };
    private List<DoanhThuNgay> _ngay = new();
    private List<MonBanChay> _mon = new();

    public ReportPanel()
    {
        Dock = DockStyle.Fill;
        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(6) };
        var btnXem = new Button { Text = "Xem thống kê", AutoSize = true, BackColor = Color.SteelBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        var btnCsv = new Button { Text = "Xuất CSV", AutoSize = true };
        btnXem.Click += (_, _) => Xem();
        btnCsv.Click += (_, _) => XuatCsv();
        bar.Controls.AddRange(new Control[] { new Label { Text = "Từ", AutoSize = true, Padding = new Padding(0, 6, 0, 0) }, dtTu, new Label { Text = "Đến", AutoSize = true, Padding = new Padding(0, 6, 0, 0) }, dtDen, btnXem, btnCsv, lblTong });

        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 420 };
        split.Panel1.Controls.Add(gridNgay); split.Panel2.Controls.Add(gridMon);
        Controls.Add(split); Controls.Add(bar);
        Xem();
    }

    private void Xem()
    {
        if (dtTu.Value.Date > dtDen.Value.Date) { MessageBox.Show("Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc."); return; }
        try
        {
            using var db = Program.NewDb();
            var svc = new OrderService(db);
            _ngay = svc.DoanhThuTheoNgay(dtTu.Value, dtDen.Value);
            _mon = svc.MonBanChay(dtTu.Value, dtDen.Value);
            gridNgay.DataSource = _ngay.Select(x => new { Ngay = x.Ngay.ToString("dd/MM/yyyy"), SoDon = x.SoDon, DoanhThu = x.DoanhThu.ToString("N0") }).ToList();
            gridMon.DataSource = _mon.Select(x => new { Mon = x.TenMon, SoLuongBan = x.SoLuong, DoanhThu = x.DoanhThu.ToString("N0") }).ToList();
            lblTong.Text = $"Tổng doanh thu: {_ngay.Sum(x => x.DoanhThu):N0} đ  |  {_ngay.Sum(x => x.SoDon)} đơn đã thanh toán";
        }
        catch (Exception ex) { MessageBox.Show(ex.GetBaseException().Message, "Lỗi thống kê", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void XuatCsv()
    {
        using var dlg = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = $"doanhthu_{DateTime.Now:yyyyMMdd}.csv" };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        var sb = new StringBuilder("Ngay,SoDon,DoanhThu\n");
        foreach (var x in _ngay) sb.AppendLine($"{x.Ngay:yyyy-MM-dd},{x.SoDon},{x.DoanhThu:0}");
        sb.AppendLine().AppendLine("Mon,SoLuongBan,DoanhThu");
        foreach (var m in _mon) sb.AppendLine($"\"{m.TenMon.Replace("\"", "\"\"")}\",{m.SoLuong},{m.DoanhThu:0}");
        File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));   // BOM để Excel hiển thị đúng tiếng Việt
        MessageBox.Show("Đã xuất file.", "Thành công");
    }
}
