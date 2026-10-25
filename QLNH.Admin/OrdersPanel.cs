using System.Drawing.Printing;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using QLNH.Data;
using QLNH.Data.Entities;
using QLNH.Data.Services;

namespace QLNH.Admin;

public class OrdersPanel : UserControl
{
    private readonly DataGridView gridDon = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, BackgroundColor = Color.White, MultiSelect = false };
    private readonly DataGridView gridCt = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, BackgroundColor = Color.White, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
    private readonly ComboBox cboLoc = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
    private readonly ComboBox cboTrangThai = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };

    public OrdersPanel()
    {
        Dock = DockStyle.Fill;
        cboLoc.Items.Add("(Tất cả)"); cboLoc.Items.AddRange(TrangThaiDon.TatCa); cboLoc.SelectedIndex = 0;
        cboTrangThai.Items.AddRange(TrangThaiDon.TatCa); cboTrangThai.SelectedIndex = 0;
        cboLoc.SelectedIndexChanged += (_, _) => Tai();

        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42, Padding = new Padding(6) };
        var btnNew = new Button { Text = "＋ Tạo đơn", AutoSize = true, BackColor = Color.SeaGreen, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
        var btnStatus = new Button { Text = "Đổi trạng thái →", AutoSize = true, Cursor = Cursors.Hand };
        var btnPrint = new Button { Text = "🖨 In hóa đơn", AutoSize = true, BackColor = Color.SteelBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
        var btnReload = new Button { Text = "⟳ Tải lại", AutoSize = true, Cursor = Cursors.Hand };

        btnNew.Click += (_, _) => { using var f = new OrderForm(); if (f.ShowDialog(this) == DialogResult.OK) Tai(); };
        btnStatus.Click += (_, _) => DoiTrangThai();
        btnPrint.Click += (_, _) => InDonDangChon();
        btnReload.Click += (_, _) => Tai();

        bar.Controls.AddRange(new Control[] { btnNew, new Label { Text = "Lọc:", AutoSize = true, Padding = new Padding(8, 6, 0, 0) }, cboLoc, btnStatus, cboTrangThai, btnPrint, btnReload });

        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 260 };

        Panel pnlGridDon = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) }; pnlGridDon.Controls.Add(gridDon);
        Panel pnlGridCt = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) }; pnlGridCt.Controls.Add(gridCt);

        split.Panel1.Controls.Add(pnlGridDon);
        split.Panel2.Controls.Add(pnlGridCt);

        gridDon.SelectionChanged += (_, _) => TaiChiTiet();
        Controls.Add(split); Controls.Add(bar);
        Tai();
    }

    public void Tai()
    {
        try
        {
            using var db = Program.NewDb();
            var q = db.DonHangs.Include(d => d.Ban).AsQueryable();
            if (cboLoc.SelectedIndex > 0) { var tt = (string)cboLoc.SelectedItem!; q = q.Where(d => d.TrangThai == tt); }
            gridDon.DataSource = q.OrderByDescending(d => d.NgayTao).AsEnumerable()
                .Select(d => new { d.Id, d.NgayTao, d.TenKhach, d.SoDienThoai, Ban = d.Ban?.TenBan ?? "Online/Mang về", d.TrangThai, TongTien = d.TongTien.ToString("N0"), d.GhiChu }).ToList();
            TaiChiTiet();
        }
        catch (Exception ex) { MessageBox.Show(ex.GetBaseException().Message, "Lỗi tải đơn hàng", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private int? IdDangChon() => gridDon.CurrentRow?.Cells["Id"].Value is int id ? id : null;

    private void TaiChiTiet()
    {
        if (IdDangChon() is not int id) { gridCt.DataSource = null; return; }
        using var db = Program.NewDb();
        gridCt.DataSource = db.ChiTietDonHangs.Include(c => c.MonAn).Where(c => c.DonHangId == id).AsEnumerable()
            .Select(c => new { Mon = c.MonAn!.TenMon, c.SoLuong, DonGia = c.DonGia.ToString("N0"), ThanhTien = c.ThanhTien.ToString("N0") }).ToList();
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
        if (IdDangChon() is not int id) { MessageBox.Show("Hãy chọn một đơn hàng để in hóa đơn."); return; }
        using var db = Program.NewDb();
        var donHang = db.DonHangs.Include(d => d.Ban).FirstOrDefault(d => d.Id == id);
        if (donHang == null) return;
        string tenBan = donHang.Ban?.TenBan ?? "Online/Mang về";
        decimal tienCoc = TachTienCoc(donHang.GhiChu);
        PrintBill(tenBan, donHang.TenKhach, donHang.TongTien, tienCoc, gridCt.Rows);
    }

    private void DoiTrangThai()
    {
        if (IdDangChon() is not int id) { MessageBox.Show("Hãy chọn một đơn hàng."); return; }

        string trangThaiMoi = (string)cboTrangThai.SelectedItem!;

        try
        {
            using var db = Program.NewDb();

            var donHang = db.DonHangs.Include(d => d.Ban).FirstOrDefault(d => d.Id == id);
            if (donHang == null) return;

            new OrderService(db).DoiTrangThai(id, trangThaiMoi);

            if (trangThaiMoi == "Đã thanh toán")
            {
                var confirmPrint = MessageBox.Show("Đã thanh toán đơn hàng thành công. Bạn có muốn in hóa đơn không?",
                                                   "In hóa đơn", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (confirmPrint == DialogResult.Yes)
                {
                    string tenBan = donHang.Ban?.TenBan ?? "Online/Mang về";
                    decimal tienCoc = TachTienCoc(donHang.GhiChu);
                    PrintBill(tenBan, donHang.TenKhach, donHang.TongTien, tienCoc, gridCt.Rows);
                }
            }

            Tai();
        }
        catch (Exception ex) { MessageBox.Show(ex.GetBaseException().Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void PrintBill(string tenBan, string tenKhach, decimal tongTien, decimal tienCoc, DataGridViewRowCollection rows)
    {
        PrintDocument pd = new PrintDocument();
        pd.PrintPage += (sender, e) =>
        {
            Graphics g = e.Graphics!;
            Font fontTitle = new Font("Segoe UI", 16, FontStyle.Bold);
            Font fontSub = new Font("Segoe UI", 12, FontStyle.Bold);
            Font fontHeader = new Font("Segoe UI", 11, FontStyle.Bold);
            Font fontNormal = new Font("Segoe UI", 11, FontStyle.Regular);

            StringFormat formatRight = new StringFormat { Alignment = StringAlignment.Far };
            StringFormat formatCenter = new StringFormat { Alignment = StringAlignment.Center };

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

        PrintPreviewDialog preview = new PrintPreviewDialog
        {
            Document = pd,
            Width = 850,
            Height = 700,
            StartPosition = FormStartPosition.CenterScreen
        };
        preview.ShowDialog();
    }
}