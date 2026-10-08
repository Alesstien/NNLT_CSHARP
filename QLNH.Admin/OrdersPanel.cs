using Microsoft.EntityFrameworkCore;
using QLNH.Data;
using QLNH.Data.Entities;
using QLNH.Data.Services;

namespace QLNH.Admin;

public class OrdersPanel : UserControl
{
    private readonly DataGridView gridDon = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, BackgroundColor = Color.White, MultiSelect = false };
    private readonly DataGridView gridCt = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, BackgroundColor = Color.White };
    private readonly ComboBox cboLoc = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };
    private readonly ComboBox cboTrangThai = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140 };

    public OrdersPanel()
    {
        Dock = DockStyle.Fill;
        cboLoc.Items.Add("(Tất cả)"); cboLoc.Items.AddRange(TrangThaiDon.TatCa); cboLoc.SelectedIndex = 0;
        cboTrangThai.Items.AddRange(TrangThaiDon.TatCa); cboTrangThai.SelectedIndex = 0;
        cboLoc.SelectedIndexChanged += (_, _) => Tai();

        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42, Padding = new Padding(6) };
        var btnNew = new Button { Text = "＋ Tạo đơn", AutoSize = true, BackColor = Color.SeaGreen, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        var btnStatus = new Button { Text = "Đổi trạng thái →", AutoSize = true };
        var btnReload = new Button { Text = "⟳ Tải lại", AutoSize = true };
        btnNew.Click += (_, _) => { using var f = new OrderForm(); if (f.ShowDialog(this) == DialogResult.OK) Tai(); };
        btnStatus.Click += (_, _) => DoiTrangThai();
        btnReload.Click += (_, _) => Tai();
        bar.Controls.AddRange(new Control[] { btnNew, new Label { Text = "Lọc:", AutoSize = true, Padding = new Padding(8, 6, 0, 0) }, cboLoc, btnStatus, cboTrangThai, btnReload });

        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 260 };
        split.Panel1.Controls.Add(gridDon); split.Panel2.Controls.Add(gridCt);
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
                .Select(d => new { d.Id, d.NgayTao, d.TenKhach, d.SoDienThoai, Ban = d.Ban?.TenBan, d.TrangThai, TongTien = d.TongTien.ToString("N0"), d.GhiChu }).ToList();
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
        try
        {
            using var db = Program.NewDb();
            new OrderService(db).DoiTrangThai(id, (string)cboTrangThai.SelectedItem!);
            Tai();
        }
        catch (Exception ex) { MessageBox.Show(ex.GetBaseException().Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
