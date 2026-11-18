using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using QLNH.Data.Services;

namespace QLNH.Admin;

public partial class ReportPanel : UserControl
{
    private readonly DateTimePicker dtTu = new()
    {
        Format = DateTimePickerFormat.Short,
        Value = DateTime.Today.AddDays(-30),
        Width = 115
    };

    private readonly DateTimePicker dtDen = new()
    {
        Format = DateTimePickerFormat.Short,
        Value = DateTime.Today,
        Width = 115
    };

    private readonly DataGridView gridNgay = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        BackgroundColor = Color.White,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        RowHeadersVisible = false
    };

    private readonly DataGridView gridMon = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        BackgroundColor = Color.White,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        RowHeadersVisible = false
    };

    private readonly Label lblKpiDoanhThu = new()
    {
        Font = new Font("Segoe UI", 18F, FontStyle.Bold),
        ForeColor = Color.White,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter
    };

    private readonly Label lblKpiSoDon = new()
    {
        Font = new Font("Segoe UI", 18F, FontStyle.Bold),
        ForeColor = Color.White,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter
    };

    private readonly Label lblKpiMonHot = new()
    {
        Font = new Font("Segoe UI", 15F, FontStyle.Bold),
        ForeColor = Color.White,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter
    };

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

        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(6),
            BackColor = Color.White
        };

        var btnXem = new Button
        {
            Text = "Áp dụng bộ lọc",
            AutoSize = true,
            BackColor = Color.SteelBlue,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };

        var btn7Ngay = new Button
        {
            Text = "7 ngày",
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };

        var btn30Ngay = new Button
        {
            Text = "30 ngày",
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };

        var btnTatCa = new Button
        {
            Text = "Tất cả",
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };

        var btnExcel = new Button
        {
            Text = "Xuất Excel",
            AutoSize = true,
            BackColor = Color.SeaGreen,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };

        // Chỉ cập nhật Dashboard khi người dùng nhấn nút Áp dụng bộ lọc.
        // Việc thay đổi ngày không tự động gọi truy vấn thống kê.
        btnXem.Click += (_, _) => Xem();
        btn7Ngay.Click += (_, _) =>
            ChonKhoangNgay(DateTime.Today.AddDays(-6), DateTime.Today);
        btn30Ngay.Click += (_, _) =>
            ChonKhoangNgay(DateTime.Today.AddDays(-30), DateTime.Today);
        btnTatCa.Click += (_, _) =>
            ChonKhoangNgay(DateTime.Today.AddYears(-1), DateTime.Today);
        btnExcel.Click += (_, _) => XuatExcel();

        bar.Controls.AddRange(new Control[]
        {
            new Label
            {
                Text = "Từ:",
                AutoSize = true,
                Padding = new Padding(0, 6, 0, 0)
            },
            dtTu,
            new Label
            {
                Text = "Đến:",
                AutoSize = true,
                Padding = new Padding(4, 6, 0, 0)
            },
            dtDen,
            btnXem,
            btn7Ngay,
            btn30Ngay,
            btnTatCa,
            btnExcel
        });

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };

        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 115F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 52F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 48F));

        var pnlKPIs = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1
        };

        pnlKPIs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        pnlKPIs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        pnlKPIs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));

        pnlKPIs.Controls.Add(
            CreateKpiCard("TỔNG DOANH THU", lblKpiDoanhThu, Color.MediumSeaGreen), 0, 0);
        pnlKPIs.Controls.Add(
            CreateKpiCard("TỔNG SỐ ĐƠN ĐÃ THANH TOÁN", lblKpiSoDon, Color.DodgerBlue), 1, 0);
        pnlKPIs.Controls.Add(
            CreateKpiCard("MÓN BÁN CHẠY NHẤT", lblKpiMonHot, Color.DarkOrange), 2, 0);

        var pnlCharts = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };

        pnlCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
        pnlCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));

        InitializeCharts();

        var wrapChart1 = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 4, 4, 4)
        };
        wrapChart1.Controls.Add(chartDoanhThu);

        var wrapChart2 = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(4, 4, 8, 4)
        };
        wrapChart2.Controls.Add(chartMonHot);

        pnlCharts.Controls.Add(wrapChart1, 0, 0);
        pnlCharts.Controls.Add(wrapChart2, 1, 0);

        var pnlGrids = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };

        pnlGrids.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
        pnlGrids.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));

        var panelGridNgay = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 4, 4, 8)
        };

        var lblGridNgay = new Label
        {
            Text = "Chi tiết doanh thu theo ngày",
            Dock = DockStyle.Top,
            Height = 26,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.DarkSlateGray
        };

        panelGridNgay.Controls.Add(gridNgay);
        panelGridNgay.Controls.Add(lblGridNgay);

        var panelGridMon = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(4, 4, 8, 8)
        };

        var lblGridMon = new Label
        {
            Text = "Xếp hạng món ăn bán chạy",
            Dock = DockStyle.Top,
            Height = 26,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.DarkSlateGray
        };

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
        dtTu.Value = tu;
        dtDen.Value = den;

        // Các nút mốc thời gian áp dụng bộ lọc ngay sau khi chọn.
        Xem();
    }

    private void InitializeCharts()
    {
        chartDoanhThu = new Chart
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            AntiAliasing = AntiAliasingStyles.All,
            TextAntiAliasingQuality = TextAntiAliasingQuality.High
        };

        var area1 = new ChartArea
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

        var seriesDoanhThu = new Series
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

        chartMonHot = new Chart
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            AntiAliasing = AntiAliasingStyles.All,
            TextAntiAliasingQuality = TextAntiAliasingQuality.High
        };

        var area2 = new ChartArea
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

        var seriesMonHot = new Series
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

    private static Panel CreateKpiCard(string title, Label lblValue, Color bgColor)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = bgColor,
            Margin = new Padding(8, 6, 8, 6)
        };

        var lblTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.WhiteSmoke,
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.MiddleCenter,
            Height = 32
        };

        card.Controls.Add(lblValue);
        card.Controls.Add(lblTitle);

        return card;
    }

    public void Xem()
    {
        if (dtTu.Value.Date > dtDen.Value.Date)
        {
            MessageBox.Show(
                "Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc.",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

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

            lblKpiDoanhThu.Text = $"{_ngay.Sum(x => x.DoanhThu):N0} đ";
            lblKpiSoDon.Text = $"{_ngay.Sum(x => x.SoDon)} đơn";

            var topMon = _mon
                .OrderByDescending(x => x.SoLuong)
                .FirstOrDefault();

            lblKpiMonHot.Text = topMon != null
                ? $"{topMon.TenMon} ({topMon.SoLuong})"
                : "Chưa có dữ liệu";

            var sDt = chartDoanhThu.Series["DoanhThu"];
            sDt.Points.Clear();

            var area1 = chartDoanhThu.ChartAreas["MainArea"];
            area1.AxisX.CustomLabels.Clear();
            area1.AxisX.LabelStyle.Angle = _ngay.Count > 8 ? -30 : 0;

            decimal maxDt = _ngay.Any()
                ? _ngay.Max(x => x.DoanhThu)
                : 0m;

            area1.AxisY.Maximum = maxDt > 0
                ? Math.Ceiling((double)maxDt * 1.18 / 100000.0) * 100000.0
                : double.NaN;

            for (int i = 0; i < _ngay.Count; i++)
            {
                var item = _ngay[i];
                int xPos = i + 1;
                int idx = sDt.Points.AddXY(xPos, item.DoanhThu);
                var pt = sDt.Points[idx];

                pt.AxisLabel = item.Ngay.ToString("dd/MM");
                pt.Label = $"{item.DoanhThu:N0}";
                pt.ToolTip =
                    $"Ngày {item.Ngay:dd/MM/yyyy}\n" +
                    $"Doanh thu: {item.DoanhThu:N0} đ\n" +
                    $"Số đơn: {item.SoDon} đơn";

                if (_ngay.Count > 1 && item.DoanhThu == maxDt && maxDt > 0)
                {
                    pt.Color = Color.FromArgb(16, 185, 129);
                    pt.BackSecondaryColor = Color.FromArgb(52, 211, 153);
                    pt.BorderColor = Color.FromArgb(5, 150, 105);
                    pt.LabelForeColor = Color.FromArgb(6, 95, 70);
                }
            }

            var sMon = chartMonHot.Series["MonHot"];
            sMon.Points.Clear();

            Color[] piePalette =
            {
                Color.FromArgb(37, 99, 235),
                Color.FromArgb(16, 185, 129),
                Color.FromArgb(245, 158, 11),
                Color.FromArgb(239, 68, 68),
                Color.FromArgb(139, 92, 246)
            };

            var top5Mon = _mon
                .OrderByDescending(m => m.SoLuong)
                .Take(5)
                .ToList();

            for (int i = 0; i < top5Mon.Count; i++)
            {
                var item = top5Mon[i];
                int idx = sMon.Points.AddXY(item.TenMon, item.SoLuong);

                sMon.Points[idx].AxisLabel = item.TenMon;
                sMon.Points[idx].LegendText = $"{item.TenMon} ({item.SoLuong})";
                sMon.Points[idx].ToolTip =
                    $"{item.TenMon}: {item.SoLuong} phần ({item.DoanhThu:N0} đ)";
                sMon.Points[idx].Color = piePalette[i % piePalette.Length];
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Lỗi tải dữ liệu thống kê: " + ex.GetBaseException().Message,
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    // =========================================
    // XUẤT BÁO CÁO EXCEL
    // =========================================

    private void XuatExcel()
    {
        using var dlg = new SaveFileDialog
        {
            Filter = "Excel Workbook (*.xlsx)|*.xlsx",
            FileName = $"BaoCao_HuongViet_{DateTime.Now:yyyyMMdd}.xlsx",
            DefaultExt = "xlsx",
            AddExtension = true,
            OverwritePrompt = true
        };

        if (dlg.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            using var workbook = new XLWorkbook();

            workbook.Properties.Title =
                "Báo cáo kinh doanh Nhà hàng Hương Việt";
            workbook.Properties.Author = "Nhà hàng Hương Việt";
            workbook.Properties.Subject =
                "Báo cáo doanh thu và món ăn bán chạy";

            // Màu sắc thương hiệu Hương Việt.
            var doColor = XLColor.FromHtml("#A51F1F");
            var vangColor = XLColor.FromHtml("#D9A441");
            var nenColor = XLColor.FromHtml("#F8F4EC");
            var vienColor = XLColor.FromHtml("#DDDDDD");
            var dongLe = XLColor.FromHtml("#F7F7F7");
            var chuColor = XLColor.FromHtml("#333333");

            decimal tongDoanhThu = _ngay.Sum(x => x.DoanhThu);
            int tongSoDon = _ngay.Sum(x => x.SoDon);

            // =========================================
            // SHEET 1: DOANH THU
            // =========================================

            var ws = workbook.Worksheets.Add("Doanh thu");
            CaiDatNenTrangTinh(ws, chuColor);

            TaoTieuDeBaoCao(
                ws,
                4,
                "BÁO CÁO DOANH THU THEO NGÀY",
                dtTu.Value.Date,
                dtDen.Value.Date,
                doColor);

            // Tổng quan: bố trí thành hai thẻ riêng biệt để nhãn và giá trị
            // không bị dính vào nhau khi mở bằng Excel hoặc WPS Office.
            ws.Range("A5:B5").Merge();
            ws.Range("A6:B6").Merge();
            ws.Range("C5:D5").Merge();
            ws.Range("C6:D6").Merge();

            ws.Cell("A5").Value = "TỔNG DOANH THU";
            ws.Cell("A6").Value = tongDoanhThu;
            ws.Cell("C5").Value = "SỐ ĐƠN ĐÃ THANH TOÁN";
            ws.Cell("C6").Value = tongSoDon;

            var tongQuanDoanhThu = ws.Range("A5:D6");
            DinhDangTongQuan(tongQuanDoanhThu, nenColor, vienColor);

            foreach (var address in new[] { "A5:B5", "C5:D5" })
            {
                var labelRange = ws.Range(address);
                labelRange.Style.Font.FontColor = doColor;
                labelRange.Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;
                labelRange.Style.Alignment.WrapText = false;
            }

            foreach (var address in new[] { "A6:B6", "C6:D6" })
            {
                var valueRange = ws.Range(address);
                valueRange.Style.Font.FontColor = doColor;
                valueRange.Style.Font.FontSize = 14;
                valueRange.Style.Font.Bold = true;
                valueRange.Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;
            }

            ws.Cell("A6").Style.NumberFormat.Format = "#,##0 \"đ\"";
            ws.Cell("C6").Style.NumberFormat.Format = "#,##0";
            ws.Row(5).Height = 25;
            ws.Row(6).Height = 31;

            // Tiêu đề bảng chi tiết.
            ws.Range("A7:C7").Merge();
            ws.Cell("A7").Value = "CHI TIẾT DOANH THU THEO NGÀY";
            DinhDangTieuDeBang(ws.Range("A7:C7"), doColor);
            ws.Row(7).Height = 25;

            ws.Cell("A8").Value = "Ngày";
            ws.Cell("B8").Value = "Số đơn đã thanh toán";
            ws.Cell("C8").Value = "Doanh thu (VNĐ)";
            DinhDangHeaderBang(ws.Range("A8:C8"), vangColor, chuColor);
            ws.Row(8).Height = 30;

            int row = 9;

            foreach (var item in _ngay.OrderBy(x => x.Ngay))
            {
                ws.Cell(row, 1).Value = item.Ngay;
                ws.Cell(row, 2).Value = item.SoDon;
                ws.Cell(row, 3).Value = item.DoanhThu;

                ws.Cell(row, 1).Style.NumberFormat.Format = "dd/MM/yyyy";
                ws.Cell(row, 2).Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;
                ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0 \"đ\"";
                ws.Cell(row, 3).Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Right;

                if ((row - 9) % 2 == 1)
                {
                    ws.Range(row, 1, row, 3).Style.Fill.BackgroundColor = dongLe;
                }

                ws.Row(row).Height = 22;
                row++;
            }

            if (_ngay.Count == 0)
            {
                ws.Range("A9:C9").Merge();
                ws.Cell("A9").Value =
                    "Không có dữ liệu doanh thu trong khoảng thời gian này.";
                ws.Range("A9:C9").Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;
                ws.Range("A9:C9").Style.Font.Italic = true;
                row = 10;
            }

            int totalRow = row;
            ws.Range(totalRow, 1, totalRow, 2).Merge();
            ws.Cell(totalRow, 1).Value = "TỔNG DOANH THU";
            ws.Cell(totalRow, 3).Value = tongDoanhThu;

            var dongTongDoanhThu = ws.Range(totalRow, 1, totalRow, 3);
            dongTongDoanhThu.Style.Fill.BackgroundColor = nenColor;
            dongTongDoanhThu.Style.Font.Bold = true;
            dongTongDoanhThu.Style.Border.TopBorder = XLBorderStyleValues.Medium;
            dongTongDoanhThu.Style.Border.TopBorderColor = doColor;

            ws.Cell(totalRow, 3).Style.NumberFormat.Format = "#,##0 \"đ\"";
            ws.Cell(totalRow, 3).Style.Font.FontColor = doColor;
            ws.Cell(totalRow, 3).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Right;
            ws.Row(totalRow).Height = 27;

            DinhDangVienBang(ws.Range(8, 1, totalRow, 3), vienColor);

            if (_ngay.Count > 0)
            {
                ws.Range(8, 1, 8 + _ngay.Count, 3).SetAutoFilter();
            }

            ws.Column(1).Width = 20;
            ws.Column(2).Width = 26;
            ws.Column(3).Width = 27;
            ws.Column(4).Width = 24;

            ws.SheetView.FreezeRows(8);
            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            ws.PageSetup.PagesWide = 1;

            // =========================================
            // SHEET 2: MÓN BÁN CHẠY
            // =========================================

            var wsMon = workbook.Worksheets.Add("Món bán chạy");
            CaiDatNenTrangTinh(wsMon, chuColor);

            TaoTieuDeBaoCao(
                wsMon,
                4,
                "BÁO CÁO THỐNG KÊ MÓN ĂN BÁN CHẠY",
                dtTu.Value.Date,
                dtDen.Value.Date,
                doColor);

            var danhSachMon = _mon
                .OrderByDescending(x => x.SoLuong)
                .ThenByDescending(x => x.DoanhThu)
                .ToList();

            var monTop = danhSachMon.FirstOrDefault();

            // Tổng quan: ghép cặp nhãn - giá trị thành hai thẻ riêng.
            // Việc gộp A:B giúp nhãn MÓN BÁN CHẠY NHẤT không bị xuống dòng
            // do cột A vốn được giữ hẹp để hiển thị STT trong bảng bên dưới.
            wsMon.Range("A5:B5").Merge();
            wsMon.Range("A6:B6").Merge();
            wsMon.Range("C5:D5").Merge();
            wsMon.Range("C6:D6").Merge();

            wsMon.Cell("A5").Value = "MÓN BÁN CHẠY NHẤT";
            wsMon.Cell("A6").Value = monTop?.TenMon ?? "Chưa có dữ liệu";
            wsMon.Cell("C5").Value = "TỔNG SỐ PHẦN BÁN";
            wsMon.Cell("C6").Value = danhSachMon.Sum(x => x.SoLuong);

            var tongQuanMon = wsMon.Range("A5:D6");
            DinhDangTongQuan(tongQuanMon, nenColor, vienColor);

            foreach (var address in new[] { "A5:B5", "C5:D5" })
            {
                var labelRange = wsMon.Range(address);
                labelRange.Style.Font.FontColor = doColor;
                labelRange.Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;
                labelRange.Style.Alignment.WrapText = false;
            }

            foreach (var address in new[] { "A6:B6", "C6:D6" })
            {
                var valueRange = wsMon.Range(address);
                valueRange.Style.Font.FontColor = doColor;
                valueRange.Style.Font.FontSize = 13;
                valueRange.Style.Font.Bold = true;
                valueRange.Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;
                valueRange.Style.Alignment.WrapText = false;
            }

            wsMon.Row(5).Height = 25;
            wsMon.Row(6).Height = 31;

            // Tiêu đề bảng món bán chạy.
            wsMon.Range("A7:D7").Merge();
            wsMon.Cell("A7").Value = "CHI TIẾT XẾP HẠNG MÓN ĂN BÁN CHẠY";
            DinhDangTieuDeBang(wsMon.Range("A7:D7"), doColor);
            wsMon.Row(7).Height = 25;

            wsMon.Cell("A8").Value = "STT";
            wsMon.Cell("B8").Value = "Tên món ăn";
            wsMon.Cell("C8").Value = "Số lượng bán";
            wsMon.Cell("D8").Value = "Doanh thu (VNĐ)";
            DinhDangHeaderBang(wsMon.Range("A8:D8"), vangColor, chuColor);
            wsMon.Row(8).Height = 30;

            row = 9;

            for (int i = 0; i < danhSachMon.Count; i++)
            {
                var item = danhSachMon[i];

                wsMon.Cell(row, 1).Value = i + 1;
                wsMon.Cell(row, 2).Value = item.TenMon;
                wsMon.Cell(row, 3).Value = item.SoLuong;
                wsMon.Cell(row, 4).Value = item.DoanhThu;

                wsMon.Cell(row, 1).Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;
                wsMon.Cell(row, 3).Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;
                wsMon.Cell(row, 4).Style.NumberFormat.Format = "#,##0 \"đ\"";
                wsMon.Cell(row, 4).Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Right;

                if (i % 2 == 1)
                {
                    wsMon.Range(row, 1, row, 4).Style.Fill.BackgroundColor = dongLe;
                }

                wsMon.Row(row).Height = 22;
                row++;
            }

            if (danhSachMon.Count == 0)
            {
                wsMon.Range("A9:D9").Merge();
                wsMon.Cell("A9").Value =
                    "Không có dữ liệu món ăn trong khoảng thời gian này.";
                wsMon.Range("A9:D9").Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;
                wsMon.Range("A9:D9").Style.Font.Italic = true;
                row = 10;
            }

            totalRow = row;
            wsMon.Range(totalRow, 1, totalRow, 3).Merge();
            wsMon.Cell(totalRow, 1).Value = "TỔNG DOANH THU MÓN ĂN";
            wsMon.Cell(totalRow, 4).Value = danhSachMon.Sum(x => x.DoanhThu);

            var dongTongMon = wsMon.Range(totalRow, 1, totalRow, 4);
            dongTongMon.Style.Fill.BackgroundColor = nenColor;
            dongTongMon.Style.Font.Bold = true;
            dongTongMon.Style.Border.TopBorder = XLBorderStyleValues.Medium;
            dongTongMon.Style.Border.TopBorderColor = doColor;

            wsMon.Cell(totalRow, 4).Style.NumberFormat.Format = "#,##0 \"đ\"";
            wsMon.Cell(totalRow, 4).Style.Font.FontColor = doColor;
            wsMon.Cell(totalRow, 4).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Right;
            wsMon.Row(totalRow).Height = 27;

            DinhDangVienBang(wsMon.Range(8, 1, totalRow, 4), vienColor);

            if (danhSachMon.Count > 0)
            {
                wsMon.Range(8, 1, 8 + danhSachMon.Count, 4).SetAutoFilter();
            }

            wsMon.Column(1).Width = 10;
            wsMon.Column(2).Width = 38;
            wsMon.Column(3).Width = 20;
            wsMon.Column(4).Width = 25;

            wsMon.SheetView.FreezeRows(8);
            wsMon.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            wsMon.PageSetup.PagesWide = 1;

            // Lưu file Excel.
            workbook.SaveAs(dlg.FileName);

            MessageBox.Show(
                "Đã xuất file Excel thành công!\n\n" +
                "File gồm 2 trang tính:\n" +
                "1. Doanh thu theo ngày.\n" +
                "2. Xếp hạng món ăn bán chạy.",
                "Xuất Excel thành công",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Không thể xuất file Excel:\n" +
                ex.GetBaseException().Message,
                "Lỗi xuất Excel",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    // Thiết lập font và màu nền chung cho từng sheet.
    private static void CaiDatNenTrangTinh(IXLWorksheet ws, XLColor chuColor)
    {
        ws.Style.Font.FontName = "Arial";
        ws.Style.Font.FontSize = 10;
        ws.Style.Font.FontColor = chuColor;
        ws.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    // Tạo phần tiêu đề và kỳ báo cáo.
    private static void TaoTieuDeBaoCao(
        IXLWorksheet ws,
        int soCot,
        string tieuDePhu,
        DateTime tuNgay,
        DateTime denNgay,
        XLColor doColor)
    {
        var tieuDeChinh = ws.Range(1, 1, 1, soCot);
        tieuDeChinh.Merge();
        ws.Cell(1, 1).Value = "NHÀ HÀNG HƯƠNG VIỆT";
        tieuDeChinh.Style.Fill.BackgroundColor = doColor;
        tieuDeChinh.Style.Font.FontColor = XLColor.White;
        tieuDeChinh.Style.Font.Bold = true;
        tieuDeChinh.Style.Font.FontSize = 18;
        tieuDeChinh.Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Center;
        ws.Row(1).Height = 36;

        var tieuDePhuRange = ws.Range(2, 1, 2, soCot);
        tieuDePhuRange.Merge();
        ws.Cell(2, 1).Value = tieuDePhu;
        tieuDePhuRange.Style.Font.Bold = true;
        tieuDePhuRange.Style.Font.FontSize = 12;
        tieuDePhuRange.Style.Font.FontColor = doColor;
        tieuDePhuRange.Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Center;
        ws.Row(2).Height = 27;

        var kyBaoCao = ws.Range(3, 1, 3, soCot);
        kyBaoCao.Merge();
        ws.Cell(3, 1).Value =
            $"Kỳ báo cáo: {tuNgay:dd/MM/yyyy} - {denNgay:dd/MM/yyyy}";
        kyBaoCao.Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Center;
        kyBaoCao.Style.Font.Italic = true;
        ws.Row(3).Height = 23;
    }

    // Định dạng khu vực KPI/tổng quan.
    private static void DinhDangTongQuan(
        IXLRange range,
        XLColor nenColor,
        XLColor vienColor)
    {
        range.Style.Fill.BackgroundColor = nenColor;
        range.Style.Font.Bold = true;
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.OutsideBorderColor = vienColor;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        range.Style.Alignment.WrapText = true;
    }

    // Định dạng tiêu đề của từng bảng.
    private static void DinhDangTieuDeBang(IXLRange range, XLColor doColor)
    {
        range.Style.Fill.BackgroundColor = doColor;
        range.Style.Font.FontColor = XLColor.White;
        range.Style.Font.Bold = true;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    // Định dạng hàng tiêu đề cột.
    private static void DinhDangHeaderBang(
        IXLRange range,
        XLColor vangColor,
        XLColor chuColor)
    {
        range.Style.Fill.BackgroundColor = vangColor;
        range.Style.Font.FontColor = chuColor;
        range.Style.Font.Bold = true;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        range.Style.Alignment.WrapText = true;
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }

    // Kẻ viền toàn bộ vùng dữ liệu.
    private static void DinhDangVienBang(IXLRange range, XLColor vienColor)
    {
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorderColor = vienColor;
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.OutsideBorderColor = vienColor;
    }
}
