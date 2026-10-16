using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting; 
using QLNH.Data.Services;

namespace QLNH.Admin
{
    public partial class ReportPanel : UserControl
    {
        private readonly DateTimePicker dtTu = new() { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-7) }; 
        private readonly DateTimePicker dtDen = new() { Format = DateTimePickerFormat.Short, Value = DateTime.Today };
        
        private readonly DataGridView gridNgay = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, BackgroundColor = Color.White, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        private readonly DataGridView gridMon = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, BackgroundColor = Color.White, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        
        private readonly Label lblKpiDoanhThu = new() { Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = Color.White, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
        private readonly Label lblKpiSoDon = new() { Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = Color.White, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
        private readonly Label lblKpiMonHot = new() { Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = Color.White, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };

        // Hai đối tượng biểu đồ
        private Chart chartDoanhThu = null!;
        private Chart chartMonHot = null!;

        private List<DoanhThuNgay> _ngay = new();
        private List<MonBanChay> _mon = new();

        public ReportPanel()
        {
            InitializeUI();
            Xem();
        }

        private void InitializeUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.WhiteSmoke;

            var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(6), BackColor = Color.White };
            var btnXem = new Button { Text = "Xem thống kê", AutoSize = true, BackColor = Color.SteelBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            var btnCsv = new Button { Text = "Xuất CSV", AutoSize = true, BackColor = Color.SeaGreen, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            
            btnXem.Click += (_, _) => Xem();
            btnCsv.Click += (_, _) => XuatCsv();
            
            bar.Controls.AddRange(new Control[] { 
                new Label { Text = "Từ", AutoSize = true, Padding = new Padding(0, 6, 0, 0) }, dtTu, 
                new Label { Text = "Đến", AutoSize = true, Padding = new Padding(0, 6, 0, 0) }, dtDen, 
                btnXem, btnCsv 
            });

            // Layout chính: 3 dòng (KPI, Biểu đồ, Bảng dữ liệu)
            var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F)); // KPI
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));   // Biểu đồ
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));   // Bảng

            // 1. Khung KPI Cards
            var pnlKPIs = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
            pnlKPIs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            pnlKPIs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            pnlKPIs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));

            pnlKPIs.Controls.Add(CreateKpiCard("TỔNG DOANH THU", lblKpiDoanhThu, Color.MediumSeaGreen), 0, 0);
            pnlKPIs.Controls.Add(CreateKpiCard("TỔNG SỐ ĐƠN", lblKpiSoDon, Color.DodgerBlue), 1, 0);
            pnlKPIs.Controls.Add(CreateKpiCard("MÓN BÁN CHẠY NHẤT", lblKpiMonHot, Color.Orange), 2, 0);

            // 2. Khung Biểu đồ
            var pnlCharts = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            pnlCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F)); // Cột biểu đồ doanh thu to hơn
            pnlCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

            InitializeCharts();
            
            Panel wrapChart1 = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            wrapChart1.Controls.Add(chartDoanhThu);
            Panel wrapChart2 = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            wrapChart2.Controls.Add(chartMonHot);

            pnlCharts.Controls.Add(wrapChart1, 0, 0);
            pnlCharts.Controls.Add(wrapChart2, 1, 0);

            // 3. Khung chứa 2 bảng dữ liệu
            var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 450 };
            Panel panelGridNgay = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            panelGridNgay.Controls.Add(gridNgay);
            Panel panelGridMon = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            panelGridMon.Controls.Add(gridMon);

            split.Panel1.Controls.Add(panelGridNgay); 
            split.Panel2.Controls.Add(panelGridMon);

            mainLayout.Controls.Add(pnlKPIs, 0, 0);
            mainLayout.Controls.Add(pnlCharts, 0, 1);
            mainLayout.Controls.Add(split, 0, 2);

            this.Controls.Add(mainLayout);
            this.Controls.Add(bar);
        }

        private void InitializeCharts()
        {
            // Cấu hình biểu đồ Doanh Thu (Bar Chart)
            chartDoanhThu = new Chart { Dock = DockStyle.Fill, BackColor = Color.White };
            ChartArea area1 = new ChartArea { Name = "MainArea" };
            area1.AxisX.MajorGrid.Enabled = false;
            area1.AxisY.MajorGrid.LineColor = Color.LightGray;
            chartDoanhThu.ChartAreas.Add(area1);
            chartDoanhThu.Titles.Add(new Title("Biểu đồ Doanh Thu", Docking.Top, new Font("Segoe UI", 12, FontStyle.Bold), Color.Black));

            Series seriesDoanhThu = new Series
            {
                Name = "DoanhThu",
                IsValueShownAsLabel = true,
                ChartType = SeriesChartType.Column,
                Color = Color.SteelBlue
            };
            
            // FIX LỖI CỘT PHÌNH TO: Ép chiều rộng cột cố định 50 pixel
            seriesDoanhThu["PixelPointWidth"] = "50"; 
            
            chartDoanhThu.Series.Add(seriesDoanhThu);

            // Cấu hình biểu đồ Món Bán Chạy (Pie Chart)
            chartMonHot = new Chart { Dock = DockStyle.Fill, BackColor = Color.White };
            ChartArea area2 = new ChartArea { Name = "PieArea" };
            chartMonHot.ChartAreas.Add(area2);
            chartMonHot.Titles.Add(new Title("Tỷ trọng Món Bán Chạy", Docking.Top, new Font("Segoe UI", 12, FontStyle.Bold), Color.Black));

            Series seriesMonHot = new Series
            {
                Name = "MonHot",
                IsValueShownAsLabel = true,
                ChartType = SeriesChartType.Doughnut,
                CustomProperties = "PieLabelStyle=Disabled" // Tắt label đè lên hình để gọn hơn
            };
            chartMonHot.Series.Add(seriesMonHot);
            chartMonHot.Legends.Add(new Legend { Docking = Docking.Bottom, Alignment = StringAlignment.Center });
        }

        private Panel CreateKpiCard(string title, Label lblValue, Color bgColor)
        {
            Panel card = new Panel { Dock = DockStyle.Fill, BackColor = bgColor, Margin = new Padding(10) };
            Label lblTitle = new Label { Text = title, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.WhiteSmoke, Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleCenter, Height = 35 };
            
            card.Controls.Add(lblValue);
            card.Controls.Add(lblTitle);
            return card;
        }

        private void Xem()
        {
            if (dtTu.Value.Date > dtDen.Value.Date) { 
                MessageBox.Show("Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc."); 
                return; 
            }

            try
            {
                using var db = Program.NewDb();
                var svc = new OrderService(db);
                
                // FIX LỖI THỜI GIAN: Bao trùm toàn bộ ngày kết thúc đến sát nửa đêm (23:59:59)
                DateTime tuNgay = dtTu.Value.Date;
                DateTime denNgay = dtDen.Value.Date.AddDays(1).AddTicks(-1); 
                
                _ngay = svc.DoanhThuTheoNgay(tuNgay, denNgay);
                _mon = svc.MonBanChay(tuNgay, denNgay);
                
                // Nạp Grid
                gridNgay.DataSource = _ngay.Select(x => new { Ngay = x.Ngay.ToString("dd/MM/yyyy"), SoDon = x.SoDon, DoanhThu = x.DoanhThu.ToString("N0") }).ToList();
                gridMon.DataSource = _mon.Select(x => new { Mon = x.TenMon, SoLuongBan = x.SoLuong, DoanhThu = x.DoanhThu.ToString("N0") }).ToList();
                
                // Nạp KPI
                lblKpiDoanhThu.Text = $"{_ngay.Sum(x => x.DoanhThu):N0} đ";
                lblKpiSoDon.Text = $"{_ngay.Sum(x => x.SoDon)} đơn";
                var topMon = _mon.OrderByDescending(x => x.SoLuong).FirstOrDefault();
                lblKpiMonHot.Text = topMon != null ? topMon.TenMon : "Chưa có DL";

                // Nạp dữ liệu vẽ biểu đồ Doanh thu
                chartDoanhThu.Series["DoanhThu"].Points.Clear();
                foreach (var item in _ngay)
                {
                    chartDoanhThu.Series["DoanhThu"].Points.AddXY(item.Ngay.ToString("dd/MM"), item.DoanhThu);
                }

                // Nạp dữ liệu vẽ biểu đồ Món bán chạy (chỉ lấy Top 5 để hình tròn không bị nát)
                chartMonHot.Series["MonHot"].Points.Clear();
                var top5Mon = _mon.OrderByDescending(m => m.SoLuong).Take(5).ToList();
                foreach (var item in top5Mon)
                {
                    chartMonHot.Series["MonHot"].Points.AddXY(item.TenMon, item.SoLuong);
                }
            }
            catch (Exception ex) 
            { 
                MessageBox.Show("Chưa có dữ liệu thống kê hoặc lỗi kết nối: " + ex.Message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information); 
            }
        }

        private void XuatCsv()
        {
            using var dlg = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = $"doanhthu_{DateTime.Now:yyyyMMdd}.csv" };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            
            var sb = new StringBuilder("Ngay,SoDon,DoanhThu\n");
            foreach (var x in _ngay) sb.AppendLine($"{x.Ngay:yyyy-MM-dd},{x.SoDon},{x.DoanhThu:0}");
            
            sb.AppendLine().AppendLine("Mon,SoLuongBan,DoanhThu");
            foreach (var m in _mon) sb.AppendLine($"\"{m.TenMon.Replace("\"", "\"\"")}\",{m.SoLuong},{m.DoanhThu:0}");
            
            File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));
            MessageBox.Show("Đã xuất file CSV thành công.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}