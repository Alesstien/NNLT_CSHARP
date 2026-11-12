using System.Drawing.Printing;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using QLNH.Data;
using QLNH.Data.Entities;
using QLNH.Data.Services;

namespace QLNH.Admin;

public class OrdersPanel : UserControl
{
    private const int KichThuocTrang = 20;
    private int trangHienTai = 1;
    private int tongTrang = 1;

    private readonly DataGridView gridDon = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        BackgroundColor = Color.White,
        MultiSelect = false
    };

    private readonly DataGridView gridCt = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        BackgroundColor = Color.White,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect
    };

    private readonly ComboBox cboLoc = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
    private readonly ComboBox cboTrangThai = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };

    private readonly DateTimePicker dtTuNgay = new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "dd/MM/yyyy",
        ShowCheckBox = true,
        Checked = false,
        Width = 135
    };

    private readonly DateTimePicker dtDenNgay = new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "dd/MM/yyyy",
        ShowCheckBox = true,
        Checked = false,
        Width = 135
    };

    private readonly TextBox txtTim = new()
    {
        Width = 220,
        PlaceholderText = "ID, mã HV, tên khách, SĐT..."
    };

    private readonly ComboBox cboBan = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 150
    };

    private readonly Label lblTongHop = new()
    {
        AutoSize = true,
        Padding = new Padding(8)
    };

    private readonly Label lblTrang = new()
    {
        AutoSize = true,
        Padding = new Padding(8)
    };

    private readonly Button btnTruoc = new()
    {
        Text = "Trước",
        AutoSize = true
    };

    private readonly Button btnSau = new()
    {
        Text = "Sau",
        AutoSize = true
    };

    public OrdersPanel()
    {
        Dock = DockStyle.Fill;

        cboLoc.Items.Add("(Tất cả)");
        cboLoc.Items.AddRange(TrangThaiDon.TatCa);
        cboLoc.SelectedIndex = 0;

        cboTrangThai.Items.AddRange(TrangThaiDon.TatCa);
        cboTrangThai.SelectedIndex = 0;

        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42, Padding = new Padding(6) };
        var btnNew = new Button { Text = "Tạo đơn", AutoSize = true, BackColor = Color.SeaGreen, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
        var btnStatus = new Button { Text = "Đổi trạng thái", AutoSize = true, Cursor = Cursors.Hand };
        var btnPrint = new Button { Text = "In hóa đơn", AutoSize = true, BackColor = Color.SteelBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
        var btnReload = new Button { Text = "Tải lại", AutoSize = true, Cursor = Cursors.Hand };

        btnNew.Click += (_, _) => { using var f = new OrderForm(); if (f.ShowDialog(this) == DialogResult.OK) Tai(); };
        btnStatus.Click += (_, _) => DoiTrangThai();
        btnPrint.Click += (_, _) => InDonDangChon();
        btnReload.Click += (_, _) => Tai();

        bar.Controls.AddRange(new Control[]
        {
            btnNew,
            btnStatus,
            cboTrangThai,
            btnPrint,
            btnReload
        });

        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 260 };

        var pnlGridDon = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
        pnlGridDon.Controls.Add(gridDon);
        var pnlGridCt = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
        pnlGridCt.Controls.Add(gridCt);

        split.Panel1.Controls.Add(pnlGridDon);
        split.Panel2.Controls.Add(pnlGridCt);

        gridDon.SelectionChanged += (_, _) => TaiChiTiet();

        using (var db = Program.NewDb())
        {
            var danhSachBan = db.Bans
                .AsNoTracking()
                .OrderBy(b => b.TenBan)
                .Select(b => new
                {
                    Id = (int?)b.Id,
                    Ten = b.TenBan
                })
                .ToList();

            danhSachBan.Insert(0, new { Id = (int?)null, Ten = "Tất cả bàn" });

            cboBan.DisplayMember = "Ten";
            cboBan.ValueMember = "Id";
            cboBan.DataSource = danhSachBan;
        }

        var btnLoc = new Button { Text = "Lọc / Tìm", AutoSize = true };
        var btnXoaLoc = new Button { Text = "Xóa lọc", AutoSize = true };
        var btnChiTiet = new Button { Text = "Xem chi tiết", AutoSize = true };

        var boLoc = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(6),
            WrapContents = true
        };

        boLoc.Controls.AddRange(new Control[]
        {
            new Label { Text = "Ngày tạo từ", AutoSize = true, Padding = new Padding(0, 6, 0, 0) },
            dtTuNgay,
            new Label { Text = "Đến hết ngày", AutoSize = true, Padding = new Padding(5, 6, 0, 0) },
            dtDenNgay,
            new Label { Text = "Trạng thái", AutoSize = true, Padding = new Padding(4, 6, 0, 0) },
            cboLoc,
            cboBan,
            txtTim,
            btnLoc,
            btnXoaLoc,
            btnChiTiet
        });

        var phanTrang = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 42,
            Padding = new Padding(6)
        };
        phanTrang.Controls.AddRange(new Control[] { btnTruoc, lblTrang, btnSau });

        var tongHop = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 38
        };
        tongHop.Controls.Add(lblTongHop);

        btnLoc.Click += (_, _) =>
        {
            trangHienTai = 1;
            Tai();
        };

        btnXoaLoc.Click += (_, _) =>
        {
            dtTuNgay.Checked = false;
            dtDenNgay.Checked = false;
            cboBan.SelectedIndex = 0;
            txtTim.Clear();

            trangHienTai = 1;
            if (cboLoc.SelectedIndex != 0)
                cboLoc.SelectedIndex = 0;
            else
                Tai();
        };

        cboLoc.SelectedIndexChanged += (_, _) =>
        {
            trangHienTai = 1;
            Tai();
        };

        txtTim.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                trangHienTai = 1;
                Tai();
            }
        };

        btnTruoc.Click += (_, _) =>
        {
            if (trangHienTai <= 1) return;
            trangHienTai--;
            Tai();
        };

        btnSau.Click += (_, _) =>
        {
            if (trangHienTai >= tongTrang) return;
            trangHienTai++;
            Tai();
        };

        btnChiTiet.Click += (_, _) => XemChiTiet();

        gridDon.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
                XemChiTiet();
        };

        Controls.Add(split);
        Controls.Add(tongHop);
        Controls.Add(phanTrang);
        Controls.Add(boLoc);
        Controls.Add(bar);

        Tai();
    }

    public void Tai()
    {
        try
        {
            if (dtTuNgay.Checked && dtDenNgay.Checked && dtTuNgay.Value.Date > dtDenNgay.Value.Date)
            {
                MessageBox.Show(
                    "Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc.",
                    "Kiểm tra bộ lọc",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            using var db = Program.NewDb();
            var query = db.DonHangs.AsNoTracking().AsQueryable();

            if (cboLoc.SelectedIndex > 0)
            {
                var trangThai = cboLoc.SelectedItem!.ToString();
                query = query.Where(d => d.TrangThai == trangThai);
            }

            if (dtTuNgay.Checked)
            {
                var tuNgay = dtTuNgay.Value.Date;
                query = query.Where(d => d.NgayTao >= tuNgay);
            }

            if (dtDenNgay.Checked)
            {
                var denNgay = dtDenNgay.Value.Date.AddDays(1);
                query = query.Where(d => d.NgayTao < denNgay);
            }

            if (cboBan.SelectedValue is int banId)
                query = query.Where(d => d.BanId == banId);

            var tuKhoa = txtTim.Text.Trim();
            if (!string.IsNullOrEmpty(tuKhoa))
            {
                var laId = int.TryParse(tuKhoa, out var id);
                query = query.Where(d =>
                    (laId && d.Id == id)
                    || (d.MaDatBan != null && d.MaDatBan.Contains(tuKhoa))
                    || d.TenKhach.Contains(tuKhoa)
                    || (d.SoDienThoai != null && d.SoDienThoai.Contains(tuKhoa))
                    || (d.GhiChu != null && d.GhiChu.Contains(tuKhoa)));
            }

            var tongDon = query.Count();
            var tongGiaTri = query.Select(d => (decimal?)d.TongTien).Sum() ?? 0;

            tongTrang = Math.Max(1, (int)Math.Ceiling(tongDon / (double)KichThuocTrang));
            trangHienTai = Math.Clamp(trangHienTai, 1, tongTrang);

            var danhSach = query
                .Include(d => d.Ban)
                .OrderByDescending(d => d.NgayTao)
                .ThenByDescending(d => d.Id)
                .Skip((trangHienTai - 1) * KichThuocTrang)
                .Take(KichThuocTrang)
                .ToList();

            gridDon.DataSource = danhSach.Select(d => new
            {
                d.Id,
                d.NgayTao,
                d.TenKhach,
                d.SoDienThoai,
                Ban = d.Ban?.TenBan ?? "Online/Mang về",
                d.TrangThai,
                TongTien = d.TongTien.ToString("N0"),
                d.GhiChu
            }).ToList();

            if (gridDon.Columns["NgayTao"] is DataGridViewColumn colNgay)
                colNgay.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";

            lblTongHop.Text = $"Theo bộ lọc: {tongDon:N0} đơn  |  Tổng giá trị đơn: {tongGiaTri:N0} đ";
            lblTrang.Text = $"Trang {trangHienTai} / {tongTrang}";
            btnTruoc.Enabled = trangHienTai > 1;
            btnSau.Enabled = trangHienTai < tongTrang;

            TaiChiTiet();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.GetBaseException().Message,
                "Lỗi tải đơn hàng",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private int? IdDangChon() => gridDon.CurrentRow?.Cells["Id"].Value is int id ? id : null;

    private void TaiChiTiet()
    {
        if (IdDangChon() is not int id)
        {
            gridCt.DataSource = null;
            return;
        }
        var ttHienTai = gridDon.CurrentRow?.Cells["TrangThai"]?.Value?.ToString();
        if (!string.IsNullOrEmpty(ttHienTai) && cboTrangThai.Items.Contains(ttHienTai))
        {
            cboTrangThai.SelectedItem = ttHienTai;
        }
        using var db = Program.NewDb();
        gridCt.DataSource = db.ChiTietDonHangs
            .Include(c => c.MonAn)
            .Where(c => c.DonHangId == id)
            .AsEnumerable()
            .Select(c => new
            {
                Mon = c.MonAn?.TenMon ?? $"Món #{c.MonAnId}",
                c.SoLuong,
                DonGia = c.DonGia.ToString("N0"),
                ThanhTien = c.ThanhTien.ToString("N0")
            })
            .ToList();
    }

    public static decimal TachTienCoc(string? ghiChu)
    {
        if (string.IsNullOrWhiteSpace(ghiChu)) return 0;
        var m = Regex.Match(ghiChu, @"Cọc:\s*([\d\.,]+)", RegexOptions.IgnoreCase);
        if (!m.Success) return 0;
        var digits = Regex.Replace(m.Groups[1].Value, @"[^\d]", "");
        return decimal.TryParse(digits, out var coc) ? coc : 0;
    }

    private void InDonDangChon()
    {
        if (IdDangChon() is not int id)
        {
            MessageBox.Show("Hãy chọn một đơn hàng để in hóa đơn.");
            return;
        }
        using var db = Program.NewDb();
        var donHang = db.DonHangs.Include(d => d.Ban).FirstOrDefault(d => d.Id == id);
        if (donHang == null) return;
        string tenBan = donHang.Ban?.TenBan ?? "Online/Mang về";
        decimal tienCoc = TachTienCoc(donHang.GhiChu);
        PrintBill(tenBan, donHang.TenKhach, donHang.TongTien, tienCoc, gridCt.Rows);
    }

    private void DoiTrangThai()
    {
        if (IdDangChon() is not int id)
        {
            MessageBox.Show("Hãy chọn một đơn hàng.");
            return;
        }

        if (cboTrangThai.SelectedItem is not string trangThaiMoi)
        {
            MessageBox.Show("Hãy chọn trạng thái cần cập nhật.");
            return;
        }

        try
        {
            using var db = Program.NewDb();
            var donHang = db.DonHangs.Include(d => d.Ban).FirstOrDefault(d => d.Id == id);
            if (donHang == null) return;

            new OrderService(db).DoiTrangThai(id, trangThaiMoi);

            if (trangThaiMoi == TrangThaiDon.DaThanhToan)
            {
                var confirmPrint = MessageBox.Show(
                    "Đã thanh toán đơn hàng thành công. Bạn có muốn in hóa đơn không?",
                    "In hóa đơn",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);
                if (confirmPrint == DialogResult.Yes)
                {
                    string tenBan = donHang.Ban?.TenBan ?? "Online/Mang về";
                    decimal tienCoc = TachTienCoc(donHang.GhiChu);
                    PrintBill(tenBan, donHang.TenKhach, donHang.TongTien, tienCoc, gridCt.Rows);
                }
            }

            Tai();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.GetBaseException().Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void XemChiTiet()
    {
        if (IdDangChon() is not int id)
        {
            MessageBox.Show("Hãy chọn một đơn hàng.");
            return;
        }

        try
        {
            using var db = Program.NewDb();
            var don = db.DonHangs
                .AsNoTracking()
                .Include(d => d.Ban)
                .Include(d => d.ChiTiets)
                    .ThenInclude(c => c.MonAn)
                .FirstOrDefault(d => d.Id == id);

            if (don == null)
            {
                MessageBox.Show("Đơn hàng không còn tồn tại.");
                return;
            }

            var tienCoc = TachTienCoc(don.GhiChu);

            using var form = new Form
            {
                Text = $"Chi tiết đơn hàng #{don.Id}",
                Size = new Size(900, 650),
                MinimumSize = new Size(650, 450),
                StartPosition = FormStartPosition.CenterParent,
                Font = new Font("Segoe UI", 10)
            };

            var thongTin = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 250,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.White,
                Text = string.Join(Environment.NewLine, new[]
                {
                    $"Mã đơn nội bộ: {don.Id}" + (!string.IsNullOrWhiteSpace(don.MaDatBan) ? $"  |  Mã đặt bàn: {don.MaDatBan}" : ""),
                    $"Ngày tạo: {don.NgayTao:dd/MM/yyyy HH:mm}",
                    $"Khách hàng: {don.TenKhach}",
                    $"Điện thoại: {don.SoDienThoai}",
                    $"Bàn: {don.Ban?.TenBan ?? "Online/Mang về"}",
                    $"Trạng thái: {don.TrangThai}",
                    $"Mã ưu đãi: {don.MaUuDai ?? "Không có"}  |  Giảm giá: {don.GiamGia:N0} đ",
                    $"Tổng giá trị đơn: {don.TongTien:N0} đ",
                    $"Tiền cọc đọc từ ghi chú: {tienCoc:N0} đ",
                    $"Ghi chú: {don.GhiChu}"
                })
            };

            var danhSachMon = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                RowHeadersVisible = false,
                DataSource = don.ChiTiets.Select(c => new
                {
                    Mon = c.MonAn?.TenMon ?? $"Món #{c.MonAnId}",
                    c.SoLuong,
                    DonGia = c.DonGia.ToString("N0"),
                    ThanhTien = c.ThanhTien.ToString("N0")
                }).ToList()
            };

            form.Controls.Add(danhSachMon);
            form.Controls.Add(thongTin);
            form.ShowDialog(this);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.GetBaseException().Message,
                "Lỗi xem chi tiết",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static void PrintBill(string tenBan, string tenKhach, decimal tongTien, decimal tienCoc, DataGridViewRowCollection rows)
    {
        using var pd = new PrintDocument();
        pd.PrintPage += (_, e) =>
        {
            Graphics g = e.Graphics!;
            using var fontTitle = new Font("Segoe UI", 16, FontStyle.Bold);
            using var fontSub = new Font("Segoe UI", 12, FontStyle.Bold);
            using var fontHeader = new Font("Segoe UI", 11, FontStyle.Bold);
            using var fontNormal = new Font("Segoe UI", 11, FontStyle.Regular);
            using var formatRight = new StringFormat { Alignment = StringAlignment.Far };
            using var formatCenter = new StringFormat { Alignment = StringAlignment.Center };

            int y = 20;
            int pageCenter = e.PageBounds.Width / 2;

            g.DrawString("NHÀ HÀNG HƯƠNG VIỆT", fontTitle, Brushes.Black, new PointF(pageCenter, y), formatCenter);
            y += 38;
            g.DrawString("HÓA ĐƠN THANH TOÁN", fontSub, Brushes.Black, new PointF(pageCenter, y), formatCenter);
            y += 36;
            g.DrawString($"Bàn: {tenBan}   |   Khách: {tenKhach}   |   Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm}", fontNormal, Brushes.Black, new PointF(20, y));
            y += 28;
            g.DrawString(new string('-', 115), fontNormal, Brushes.Black, new PointF(20, y));
            y += 20;

            int colTenMon = 20;
            int colSL = 430;
            int colGia = 580;
            int colThanhTien = 750;

            g.DrawString("Tên món", fontHeader, Brushes.Black, new PointF(colTenMon, y));
            g.DrawString("SL", fontHeader, Brushes.Black, new PointF(colSL, y), formatRight);
            g.DrawString("Đơn giá", fontHeader, Brushes.Black, new PointF(colGia, y), formatRight);
            g.DrawString("Thành tiền", fontHeader, Brushes.Black, new PointF(colThanhTien, y), formatRight);
            y += 28;
            g.DrawString(new string('-', 115), fontNormal, Brushes.Black, new PointF(20, y));
            y += 20;

            decimal tamTinhMon = 0;
            foreach (DataGridViewRow row in rows)
            {
                g.DrawString(row.Cells["Mon"].Value?.ToString() ?? "", fontNormal, Brushes.Black, new PointF(colTenMon, y));
                g.DrawString(row.Cells["SoLuong"].Value?.ToString() ?? "", fontNormal, Brushes.Black, new PointF(colSL, y), formatRight);
                g.DrawString(row.Cells["DonGia"].Value?.ToString() ?? "", fontNormal, Brushes.Black, new PointF(colGia, y), formatRight);
                var ttStr = row.Cells["ThanhTien"].Value?.ToString() ?? "0";
                g.DrawString(ttStr, fontNormal, Brushes.Black, new PointF(colThanhTien, y), formatRight);
                var digits = Regex.Replace(ttStr, @"[^\d]", "");
                if (decimal.TryParse(digits, out var ttVal)) tamTinhMon += ttVal;
                y += 28;
            }

            g.DrawString(new string('-', 115), fontNormal, Brushes.Black, new PointF(20, y));
            y += 26;

            if (tamTinhMon > 0 && tamTinhMon != tongTien)
            {
                g.DrawString($"Tạm tính tiền món: {tamTinhMon:N0} đ", fontNormal, Brushes.Black, new PointF(colThanhTien, y), formatRight);
                y += 26;
                var chenhLech = tongTien - tamTinhMon;
                if (chenhLech > 0)
                {
                    g.DrawString($"Phụ thu khu vực / VAT / Phí phục vụ: +{chenhLech:N0} đ", fontNormal, Brushes.Black, new PointF(colThanhTien, y), formatRight);
                    y += 26;
                }
                else if (chenhLech < 0)
                {
                    g.DrawString($"Ưu đãi giảm giá: {chenhLech:N0} đ", fontNormal, Brushes.SeaGreen, new PointF(colThanhTien, y), formatRight);
                    y += 26;
                }
            }

            g.DrawString($"TỔNG HÓA ĐƠN: {tongTien:N0} đ", fontSub, Brushes.Black, new PointF(colThanhTien, y), formatRight);
            y += 30;

            if (tienCoc > 0)
            {
                decimal conPhaiThu = Math.Max(0, tongTien - tienCoc);
                g.DrawString($"Đã đặt cọc trước: - {tienCoc:N0} đ", fontHeader, Brushes.SeaGreen, new PointF(colThanhTien, y), formatRight);
                y += 30;
                g.DrawString($"CÒN PHẢI THANH TOÁN: {conPhaiThu:N0} đ", fontTitle, Brushes.Firebrick, new PointF(colThanhTien, y), formatRight);
            }
            else
            {
                g.DrawString($"THÀNH TIỀN THANH TOÁN: {tongTien:N0} đ", fontTitle, Brushes.Firebrick, new PointF(colThanhTien, y), formatRight);
            }
        };

        using var preview = new PrintPreviewDialog
        {
            Document = pd,
            Width = 850,
            Height = 700,
            StartPosition = FormStartPosition.CenterScreen
        };
        preview.ShowDialog();
    }
}
