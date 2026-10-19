using Microsoft.EntityFrameworkCore;
using QLNH.Data;
using QLNH.Data.Entities;
using QLNH.Data.Services;
using System.Drawing.Printing; // Thêm thư viện in ấn

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
        var btnReload = new Button { Text = "⟳ Tải lại", AutoSize = true, Cursor = Cursors.Hand };
        
        btnNew.Click += (_, _) => { using var f = new OrderForm(); if (f.ShowDialog(this) == DialogResult.OK) Tai(); };
        btnStatus.Click += (_, _) => DoiTrangThai();
        btnReload.Click += (_, _) => Tai();
        
        bar.Controls.AddRange(new Control[] { btnNew, new Label { Text = "Lọc:", AutoSize = true, Padding = new Padding(8, 6, 0, 0) }, cboLoc, btnStatus, cboTrangThai, btnReload });

        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 260 };
        
        // Trang trí thêm padding cho lưới dễ nhìn
        Panel pnlGridDon = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) }; pnlGridDon.Controls.Add(gridDon);
        Panel pnlGridCt = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) }; pnlGridCt.Controls.Add(gridCt);
        
        split.Panel1.Controls.Add(pnlGridDon); 
        split.Panel2.Controls.Add(pnlGridCt);
        
        gridDon.SelectionChanged += (_, _) => TaiChiTiet();
        Controls.Add(split); Controls.Add(bar);
        Tai();
    }

    private void Tai()
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

    private void DoiTrangThai()
    {
        if (IdDangChon() is not int id) { MessageBox.Show("Hãy chọn một đơn hàng."); return; }
        
        string trangThaiMoi = (string)cboTrangThai.SelectedItem!;
        
        try
        {
            using var db = Program.NewDb();
            
            // Lấy thông tin đơn hàng hiện tại để phục vụ việc in hóa đơn
            var donHang = db.DonHangs.Include(d => d.Ban).FirstOrDefault(d => d.Id == id);
            if (donHang == null) return;

            // Gọi logic đổi trạng thái của nhóm
            new OrderService(db).DoiTrangThai(id, trangThaiMoi);

            // LOGIC CỦA TV3: Nếu trạng thái mới là Đã thanh toán, gọi Form in hóa đơn
            if (trangThaiMoi == "Đã thanh toán")
            {
                var confirmPrint = MessageBox.Show("Đã thanh toán đơn hàng thành công. Bạn có muốn in hóa đơn không?", 
                                                   "In hóa đơn", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (confirmPrint == DialogResult.Yes)
                {
                    string tenBan = donHang.Ban?.TenBan ?? "Online/Mang về";
                    PrintBill(tenBan, donHang.TongTien, gridCt.Rows);
                }
            }

            // Tải lại SAU khi in: Tai() làm lưới chọn lại dòng đầu, nếu gọi trước thì hóa đơn in nhầm đơn khác
            Tai();
        }
        catch (Exception ex) { MessageBox.Show(ex.GetBaseException().Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    // Hàm xuất hóa đơn giống hệt bên POS
    private void PrintBill(string tenBan, decimal tongTien, DataGridViewRowCollection rows)
    {
        PrintDocument pd = new PrintDocument();
        pd.PrintPage += (sender, e) =>
        {
            Graphics g = e.Graphics!;
            Font fontTitle = new Font("Segoe UI", 16, FontStyle.Bold);
            Font fontHeader = new Font("Segoe UI", 11, FontStyle.Bold);
            Font fontNormal = new Font("Segoe UI", 11, FontStyle.Regular);
            int y = 20;

            g.DrawString("NHÀ HÀNG ĐẠI HỌC SÀI GÒN", fontTitle, Brushes.Black, new PointF(230, y));
            y += 40;
            g.DrawString("HÓA ĐƠN THANH TOÁN", fontTitle, Brushes.Black, new PointF(260, y));
            y += 40;
            g.DrawString($"Bàn: {tenBan}   |   Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm}", fontNormal, Brushes.Black, new PointF(20, y));
            y += 30;
            g.DrawString(new string('-', 100), fontNormal, Brushes.Black, new PointF(20, y));
            y += 20;
            
            g.DrawString("Tên món", fontHeader, Brushes.Black, new PointF(20, y));
            g.DrawString("SL", fontHeader, Brushes.Black, new PointF(350, y));
            g.DrawString("Đơn giá", fontHeader, Brushes.Black, new PointF(450, y));
            g.DrawString("Thành tiền", fontHeader, Brushes.Black, new PointF(600, y));
            y += 30;
            g.DrawString(new string('-', 100), fontNormal, Brushes.Black, new PointF(20, y));
            y += 20;

            foreach (DataGridViewRow row in rows)
            {
                // Sử dụng đúng tên cột của gridCt
                g.DrawString(row.Cells["Mon"].Value.ToString()!, fontNormal, Brushes.Black, new PointF(20, y));
                g.DrawString(row.Cells["SoLuong"].Value.ToString()!, fontNormal, Brushes.Black, new PointF(350, y));
                g.DrawString(row.Cells["DonGia"].Value.ToString()!, fontNormal, Brushes.Black, new PointF(450, y));
                g.DrawString(row.Cells["ThanhTien"].Value.ToString()!, fontNormal, Brushes.Black, new PointF(600, y));
                y += 30;
            }

            g.DrawString(new string('-', 100), fontNormal, Brushes.Black, new PointF(20, y));
            y += 30;
            g.DrawString($"TỔNG CỘNG: {tongTien:N0} đ", fontTitle, Brushes.Firebrick, new PointF(450, y));
        };

        PrintPreviewDialog preview = new PrintPreviewDialog
        {
            Document = pd,
            Width = 800,
            Height = 600,
            StartPosition = FormStartPosition.CenterScreen
        };
        preview.ShowDialog();
    }
}