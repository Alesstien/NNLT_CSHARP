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
        private readonly DateTimePicker dtTu = new() { Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30), Width = 115 };
        private readonly DateTimePicker dtDen = new() { Format = DateTimePickerFormat.Short, Value = DateTime.Today, Width = 115 };
        private bool _updatingDates = false;

        private readonly DataGridView gridNgay = new()
        {
            Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
            BackgroundColor = Color.White, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false
        };
        private readonly DataGridView gridMon = new()
        {
            Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
            BackgroundColor = Color.White, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false
        };

        private readonly Label lblKpiDoanhThu = new() { Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = Color.White, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
        private readonly Label lblKpiSoDon = new() { Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = Color.White, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
        private readonly Label lblKpiMonHot = new() { Font = new Font("Segoe UI", 15F, FontStyle.Bold), ForeColor = Color.White, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };

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
            var btnXem = new Button { Text = "🔍 Xem thống kê", AutoSize = true, BackColor = Color.SteelBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            var btn7Ngay = new Button { Text = "7 ngày", AutoSize = true, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            var btn30Ngay = new Button { Text = "30 ngày", AutoSize = true, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            var btnTatCa = new Button { Text = "Tất cả", AutoSize = true, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            var btnCsv = new Button { Text = "📥 Xuất CSV", AutoSize = true, BackColor = Color.SeaGreen, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };

            dtTu.ValueChanged += (_, _) => { if (!_updatingDates) Xem(); };
            dtDen.ValueChanged += (_, _) => { if (!_updatingDates) Xem(); };
            btnXem.Click += (_, _) => Xem();
            btn7Ngay.Click += (_, _) => ChonKhoangNgay(DateTime.Today.AddDays(-6), DateTime.Today);
            btn30Ngay.Click += (_, _) => ChonKhoangNgay(DateTime.Today.AddDays(-30), DateTime.Today);
            btnTatCa.Click += (_, _) => ChonKhoangNgay(DateTime.Today.AddYears(-1), DateTime.Today);
            btnCsv.Click += (_, _) => XuatCsv();

            bar.Controls.AddRange(new Control[]
            {
                new Label { Text = "Từ:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) }, dtTu,
                new Label { Text = "Đến:", AutoSize = true, Padding = new Padding(4, 6, 0, 0) }, dtDen,
                btnXem, btn7Ngay, btn30Ngay, btnTatCa, btnCsv
            });

            // Layout chính: 3 dòng (KPI, Biểu đồ, Bảng dữ liệu)
            var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 115F)); // KPI
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 52F));   // Biểu đồ
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 48F));   // Bảng

            // 1. Khung KPI Cards
            var pnlKPIs = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
            pnlKPIs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            pnlKPIs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            pnlKPIs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));

            pnlKPIs.Controls.Add(CreateKpiCard("TỔNG DOANH THU", lblKpiDoanhThu, Color.MediumSeaGreen), 0, 0);
            pnlKPIs.Controls.Add(CreateKpiCard("TỔNG SỐ ĐƠN ĐÃ THANH TOÁN", lblKpiSoDon, Color.DodgerBlue), 1, 0);
            pnlKPIs.Controls.Add(CreateKpiCard("MÓN BÁN CHẠY NHẤT", lblKpiMonHot, Color.DarkOrange), 2, 0);

            // 2. Khung Biểu đồ
            var pnlCharts = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            pnlCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            pnlCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));

            InitializeCharts();

            Panel wrapChart1 = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 4, 4, 4) };
            wrapChart1.Controls.Add(chartDoanhThu);
            Panel wrapChart2 = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4, 4, 8, 4) };
            wrapChart2.Controls.Add(chartMonHot);

            pnlCharts.Controls.Add(wrapChart1, 0, 0);
            pnlCharts.Controls.Add(wrapChart2, 1, 0);

            // 3. Khung chứa 2 bảng dữ liệu (chia 2 cột cân đối với 2 biểu đồ phía trên)
            var pnlGrids = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            pnlGrids.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            pnlGrids.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));

            Panel panelGridNgay = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 4, 4, 8) };
            var lblGridNgay = new Label { Text = "📅 Chi tiết doanh thu theo ngày", Dock = DockStyle.Top, Height = 26, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.DarkSlateGray };
            panelGridNgay.Controls.Add(gridNgay);
            panelGridNgay.Controls.Add(lblGridNgay);

            Panel panelGridMon = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4, 4, 8, 8) };
            var lblGridMon = new Label { Text = "🏆 Xếp hạng món ăn bán chạy", Dock = DockStyle.Top, Height = 26, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.DarkSlateGray };
            panelGridMon.Controls.Add(gridMon);
            panelGridMon.Controls.Add(lblGridMon);

            pnlGrids.Controls.Add(panelGridNgay, 0, 0);
            pnlGrids.Controls.Add(panelGridMon, 1, 0);

            mainLayout.Controls.Add(pnlKPIs, 0, 0);
            mainLayout.Controls.Add(pnlCharts, 0, 1);
            mainLayout.Controls.Add(pnlGrids, 0, 2);

            this.Controls.Add(mainLayout);
            this.Controls.Add(bar);
        }

        private void ChonKhoangNgay(DateTime tu, DateTime den)
        {
            _updatingDates = true;
            dtTu.Value = tu;
            dtDen.Value = den;
            _updatingDates = false;
            Xem();
        }

        private void InitializeCharts()
        {
            // Cấu hình biểu đồ Doanh Thu (Column Chart)
            chartDoanhThu = new Chart
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                AntiAliasing = AntiAliasingStyles.All,
                TextAntiAliasingQuality = TextAntiAliasingQuality.High
            };
            ChartArea area1 = new ChartArea
            {
                Name = "MainArea",
                BackColor = Color.FromArgb(248, 250, 252)
            };
            area1.AxisX.MajorGrid.Enabled = false;
            area1.AxisX.Interval = 1;
            area1.AxisX.LineColor = Color.FromArgb(148, 163, 184);
            area1.AxisX.MajorTickMark.LineColor = Color.FromArgb(148, 163, 184);
            area1.AxisX.LabelStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            area1.AxisX.LabelStyle.ForeColor = Color.FromArgb(51, 65, 85);
            area1.AxisX.IsMarginVisible = true;

            area1.AxisY.MajorGrid.LineColor = Color.FromArgb(226, 232, 240);
            area1.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
            area1.AxisY.LineColor = Color.FromArgb(148, 163, 184);
            area1.AxisY.MajorTickMark.LineColor = Color.FromArgb(148, 163, 184);
            area1.AxisY.LabelStyle.Format = "N0";
            area1.AxisY.LabelStyle.Font = new Font("Segoe UI", 8.5F);
            area1.AxisY.LabelStyle.ForeColor = Color.FromArgb(71, 85, 105);
            area1.AxisY.Minimum = 0;

            chartDoanhThu.ChartAreas.Add(area1);
            chartDoanhThu.Titles.Add(new Title(
                "Biểu đồ Doanh Thu Theo Ngày (VNĐ)",
                Docking.Top,
                new Font("Segoe UI", 12F, FontStyle.Bold),
                Color.FromArgb(30, 41, 59)));

            Series seriesDoanhThu = new Series
            {
                Name = "DoanhThu",
                IsValueShownAsLabel = true,
                IsXValueIndexed = true,
                XValueType = ChartValueType.Int32,
                LabelFormat = "N0",
                ChartType = SeriesChartType.Column,
                Color = Color.FromArgb(37, 99, 235),
                BackSecondaryColor = Color.FromArgb(96, 165, 250),
                BackGradientStyle = GradientStyle.TopBottom,
                BorderColor = Color.FromArgb(29, 78, 216),
                BorderWidth = 1,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                LabelForeColor = Color.FromArgb(30, 41, 59)
            };
            seriesDoanhThu.SmartLabelStyle.Enabled = false;
            seriesDoanhThu["LabelStyle"] = "Top";
            seriesDoanhThu["PointWidth"] = "0.55";
            seriesDoanhThu["MaxPixelPointWidth"] = "48";
            chartDoanhThu.Series.Add(seriesDoanhThu);

            // Cấu hình biểu đồ Món Bán Chạy (Doughnut Chart)
            chartMonHot = new Chart
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                AntiAliasing = AntiAliasingStyles.All,
                TextAntiAliasingQuality = TextAntiAliasingQuality.High
            };
            ChartArea area2 = new ChartArea
            {
                Name = "PieArea",
                BackColor = Color.White
            };
            chartMonHot.ChartAreas.Add(area2);
            chartMonHot.Titles.Add(new Title(
                "Tỷ trọng Top 5 Món Bán Chạy",
                Docking.Top,
                new Font("Segoe UI", 12F, FontStyle.Bold),
                Color.FromArgb(30, 41, 59)));

            Series seriesMonHot = new Series
            {
                Name = "MonHot",
                IsValueShownAsLabel = true,
                ChartType = SeriesChartType.Doughnut,
                Label = "#PERCENT{P0}",
                LegendText = "#VALX (#VALY phần)",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                LabelForeColor = Color.White,
                BorderColor = Color.White,
                BorderWidth = 2
            };
            seriesMonHot["DoughnutRadius"] = "58";
            chartMonHot.Series.Add(seriesMonHot);
            chartMonHot.Legends.Add(new Legend
            {
                Docking = Docking.Bottom,
                Alignment = StringAlignment.Center,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(51, 65, 85)
            });
        }

        private Panel CreateKpiCard(string title, Label lblValue, Color bgColor)
        {
            Panel card = new Panel { Dock = DockStyle.Fill, BackColor = bgColor, Margin = new Padding(8, 6, 8, 6) };
            Label lblTitle = new Label
            {
                Text = title, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.WhiteSmoke, Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter, Height = 32
            };

            card.Controls.Add(lblValue);
            card.Controls.Add(lblTitle);
            return card;
        }

        public void Xem()
        {
            if (dtTu.Value.Date > dtDen.Value.Date)
            {
                MessageBox.Show("Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using var db = Program.NewDb();
                var svc = new OrderService(db);

                DateTime tuNgay = dtTu.Value.Date;
                DateTime denNgay = dtDen.Value.Date;

                _ngay = svc.DoanhThuTheoNgay(tuNgay, denNgay);
                _mon = svc.MonBanChay(tuNgay, denNgay);

                // Nạp Grid
                gridNgay.DataSource = _ngay.Select(x => new
                {
                    Ngày = x.Ngay.ToString("dd/MM/yyyy"),
                    Số_Đơn = x.SoDon,
                    Doanh_Thu = $"{x.DoanhThu:N0} đ"
                }).ToList();

                gridMon.DataSource = _mon.Select(x => new
                {
                    Tên_Món = x.TenMon,
                    SL_Bán = x.SoLuong,
                    Doanh_Thu = $"{x.DoanhThu:N0} đ"
                }).ToList();

                // Nạp KPI
                lblKpiDoanhThu.Text = $"{_ngay.Sum(x => x.DoanhThu):N0} đ";
                lblKpiSoDon.Text = $"{_ngay.Sum(x => x.SoDon)} đơn";
                var topMon = _mon.OrderByDescending(x => x.SoLuong).FirstOrDefault();
                lblKpiMonHot.Text = topMon != null ? $"{topMon.TenMon} ({topMon.SoLuong})" : "Chưa có dữ liệu";

                // Nạp dữ liệu vẽ biểu đồ Doanh thu
                var sDt = chartDoanhThu.Series["DoanhThu"];
                sDt.Points.Clear();
                var area1 = chartDoanhThu.ChartAreas["MainArea"];
                area1.AxisX.CustomLabels.Clear();
                area1.AxisX.LabelStyle.Angle = _ngay.Count > 8 ? -30 : 0;

                decimal maxDt = _ngay.Any() ? _ngay.Max(x => x.DoanhThu) : 0m;
                area1.AxisY.Maximum = maxDt > 0 ? Math.Ceiling((double)maxDt * 1.18 / 100000.0) * 100000.0 : double.NaN;

                for (int i = 0; i < _ngay.Count; i++)
                {
                    var item = _ngay[i];
                    int xPos = i + 1;
                    int idx = sDt.Points.AddXY(xPos, item.DoanhThu);
                    var pt = sDt.Points[idx];
                    pt.AxisLabel = item.Ngay.ToString("dd/MM");
                    pt.Label = $"{item.DoanhThu:N0}";
                    pt.ToolTip = $"Ngày {item.Ngay:dd/MM/yyyy}\nDoanh thu: {item.DoanhThu:N0} đ\nSố đơn: {item.SoDon} đơn";

                    // Tô nổi bật cột có doanh thu cao nhất bằng màu xanh ngọc (Emerald)
                    if (_ngay.Count > 1 && item.DoanhThu == maxDt && maxDt > 0)
                    {
                        pt.Color = Color.FromArgb(16, 185, 129);
                        pt.BackSecondaryColor = Color.FromArgb(52, 211, 153);
                        pt.BorderColor = Color.FromArgb(5, 150, 105);
                        pt.LabelForeColor = Color.FromArgb(6, 95, 70);
                    }
                }

                // Nạp dữ liệu vẽ biểu đồ Món bán chạy (Top 5)
                var sMon = chartMonHot.Series["MonHot"];
                sMon.Points.Clear();
                Color[] piePalette =
                {
                    Color.FromArgb(37, 99, 235),   // Blue
                    Color.FromArgb(16, 185, 129),  // Emerald
                    Color.FromArgb(245, 158, 11),  // Amber
                    Color.FromArgb(239, 68, 68),   // Rose/Red
                    Color.FromArgb(139, 92, 246)   // Purple
                };
                var top5Mon = _mon.OrderByDescending(m => m.SoLuong).Take(5).ToList();
                for (int i = 0; i < top5Mon.Count; i++)
                {
                    var item = top5Mon[i];
                    int idx = sMon.Points.AddXY(item.TenMon, item.SoLuong);
                    sMon.Points[idx].AxisLabel = item.TenMon;
                    sMon.Points[idx].LegendText = $"{item.TenMon} ({item.SoLuong})";
                    sMon.Points[idx].ToolTip = $"{item.TenMon}: {item.SoLuong} phần ({item.DoanhThu:N0} đ)";
                    sMon.Points[idx].Color = piePalette[i % piePalette.Length];
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu thống kê: " + ex.GetBaseException().Message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void XuatCsv()
{
    using var dlg = new SaveFileDialog
    {
        Filter = "CSV (*.csv)|*.csv",
        FileName = $"doanhthu_{DateTime.Now:yyyyMMdd}.csv"
    };

    if (dlg.ShowDialog() != DialogResult.OK)
        return;

    var sb = new StringBuilder();

    // Bảng 1 — Doanh thu theo ngày tạo đơn
    sb.AppendLine("BẢNG 1 - DOANH THU THEO NGÀY TẠO ĐƠN;;");
    sb.AppendLine("Ngày;Số đơn đã thanh toán;Doanh thu (VNĐ)");

    foreach (var x in _ngay)
    {
        sb.AppendLine(FormattableString.Invariant(
            $"{x.Ngay:yyyy-MM-dd};{x.SoDon};{x.DoanhThu:0}"));
    }

    sb.AppendLine();

    // Bảng 2 — Món bán chạy
    sb.AppendLine("BẢNG 2 - MÓN BÁN CHẠY;;");
    sb.AppendLine("Tên món;Số lượng bán;Tiền món (VNĐ)");

    foreach (var m in _mon)
    {
        var tenMon = m.TenMon.Replace("\"", "\"\"");
        sb.AppendLine(FormattableString.Invariant(
            $"\"{tenMon}\";{m.SoLuong};{m.DoanhThu:0}"));
    }

    File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));

    MessageBox.Show(
        "Đã xuất báo cáo doanh thu và món bán chạy.",
        "Thành công",
        MessageBoxButtons.OK,
        MessageBoxIcon.Information);
}
    }
}
