using Microsoft.EntityFrameworkCore;
using QLNH.Data;
using QLNH.Data.Entities;
using QLNH.Data.Services;

namespace QLNH.Admin;
//OrderForm.cs / OrdersPanel.cs: Giao diện xử lý đơn hàng (tạo đơn mới, xem danh sách đơn hàng từ khách, cập nhật trạng thái phục vụ/thanh toán).
/// <summary>Hộp thoại tạo đơn hàng tại quầy.</summary>
public class OrderForm : Form
{
    private readonly AppDbContext _db = Program.NewDb();
    private readonly TextBox txtTen = new() { Width = 200 }, txtSdt = new() { Width = 200 }, txtGhiChu = new() { Width = 200 };
    private readonly ComboBox cboBan = new() { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox cboMon = new() { Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown numSl = new() { Minimum = 1, Maximum = 100, Value = 1, Width = 60 };
    private readonly DataGridView grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AutoGenerateColumns = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, BackgroundColor = Color.White };
    private readonly Label lblTong = new() { AutoSize = true, Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Color.Firebrick };
    private readonly List<(MonAn Mon, int Sl)> _dong = new();

    public OrderForm()
    {
        Text = "Tạo đơn hàng mới"; Size = new Size(640, 560); StartPosition = FormStartPosition.CenterParent;

        var bans = _db.Bans.Where(b => b.TrangThai == "Trống").AsNoTracking().OrderBy(b => b.TenBan).ToList();
        bans.Insert(0, new Ban { Id = 0, TenBan = "(Mang về / không chọn bàn)" });
        cboBan.DataSource = bans; cboBan.DisplayMember = "TenBan"; cboBan.ValueMember = "Id";
        cboMon.DataSource = _db.MonAns.Where(m => m.ConPhucVu).AsNoTracking().OrderBy(m => m.TenMon).ToList();
        cboMon.DisplayMember = "TenMon"; cboMon.ValueMember = "Id";

        var top = new TableLayoutPanel { Dock = DockStyle.Top, Height = 120, ColumnCount = 4, Padding = new Padding(8) };
        top.Controls.Add(new Label { Text = "Tên khách", AutoSize = true }, 0, 0); top.Controls.Add(txtTen, 1, 0);
        top.Controls.Add(new Label { Text = "SĐT", AutoSize = true }, 2, 0); top.Controls.Add(txtSdt, 3, 0);
        top.Controls.Add(new Label { Text = "Bàn", AutoSize = true }, 0, 1); top.Controls.Add(cboBan, 1, 1);
        top.Controls.Add(new Label { Text = "Ghi chú", AutoSize = true }, 2, 1); top.Controls.Add(txtGhiChu, 3, 1);

        var addBar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(6) };
        var btnAdd = new Button { Text = "＋ Thêm món", AutoSize = true };
        var btnRemove = new Button { Text = "Bỏ dòng chọn", AutoSize = true };
        btnAdd.Click += (_, _) => ThemMon();
        btnRemove.Click += (_, _) => BoMon();
        addBar.Controls.AddRange(new Control[] { new Label { Text = "Món", AutoSize = true, Padding = new Padding(0, 6, 0, 0) }, cboMon, new Label { Text = "SL", AutoSize = true, Padding = new Padding(0, 6, 0, 0) }, numSl, btnAdd, btnRemove });

        var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 46, Padding = new Padding(6), FlowDirection = FlowDirection.RightToLeft };
        var btnSave = new Button { Text = "Lưu đơn", Width = 100, BackColor = Color.SeaGreen, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        var btnCancel = new Button { Text = "Huỷ", Width = 80 };
        btnSave.Click += (_, _) => Luu();
        btnCancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
        bottom.Controls.AddRange(new Control[] { btnSave, btnCancel, lblTong });

        Controls.Add(grid); Controls.Add(addBar); Controls.Add(top); Controls.Add(bottom);
        Hienthi();
    }

    private void ThemMon()
    {
        if (cboMon.SelectedItem is not MonAn m) return;
        int i = _dong.FindIndex(x => x.Mon.Id == m.Id);
        if (i >= 0) _dong[i] = (m, _dong[i].Sl + (int)numSl.Value); else _dong.Add((m, (int)numSl.Value));
        Hienthi();
    }

    private void BoMon()
    {
        if (grid.CurrentRow == null || grid.CurrentRow.Index >= _dong.Count) return;
        _dong.RemoveAt(grid.CurrentRow.Index); Hienthi();
    }

    private void Hienthi()
    {
        grid.DataSource = _dong.Select(d => new { Mon = d.Mon.TenMon, SoLuong = d.Sl, DonGia = d.Mon.Gia.ToString("N0"), ThanhTien = (d.Mon.Gia * d.Sl).ToString("N0") }).ToList();
        lblTong.Text = "Tổng: " + _dong.Sum(d => d.Mon.Gia * d.Sl).ToString("N0") + " đ   ";
    }

    private void Luu()
    {
        try
        {
            int? banId = cboBan.SelectedValue is int id && id > 0 ? id : null;
            new OrderService(_db).TaoDon(txtTen.Text, txtSdt.Text, banId, txtGhiChu.Text, _dong.Select(d => new DongDat(d.Mon.Id, d.Sl)));
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        { MessageBox.Show(ex.Message, "Không thể tạo đơn", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        catch (Exception ex)
        { MessageBox.Show(ex.GetBaseException().Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    protected override void OnFormClosed(FormClosedEventArgs e) { _db.Dispose(); base.OnFormClosed(e); }
}
