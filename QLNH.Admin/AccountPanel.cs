using Microsoft.EntityFrameworkCore;
using QLNH.Data;
using QLNH.Data.Entities;
using QLNH.Data.Services;

namespace QLNH.Admin;
// AccountPanel.cs: Giao diện phụ trách quản lý tài khoản người dùng (xem, thêm, sửa, phân quyền nhân viên).
/// <summary>Quản lý tài khoản & phân quyền (chỉ Admin).</summary>
public class AccountPanel : UserControl
{
    private readonly DataGridView grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, BackgroundColor = Color.White, MultiSelect = false };
    private readonly int _currentUserId;

    public AccountPanel(int currentUserId)
    {
        _currentUserId = currentUserId; Dock = DockStyle.Fill;
        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42, Padding = new Padding(6) };
        var btnAdd = new Button { Text = "＋ Thêm tài khoản", AutoSize = true, BackColor = Color.SeaGreen, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        var btnReset = new Button { Text = "Đặt lại mật khẩu", AutoSize = true };
        var btnLock = new Button { Text = "Khoá / Mở khoá", AutoSize = true };
        btnAdd.Click += (_, _) => Them();
        btnReset.Click += (_, _) => DatLaiMatKhau();
        btnLock.Click += (_, _) => Khoa();
        bar.Controls.AddRange(new Control[] { btnAdd, btnReset, btnLock });
        Controls.Add(grid); Controls.Add(bar);
        Tai();
    }

    private void Tai()
    {
        using var db = Program.NewDb();
        grid.DataSource = db.TaiKhoans.Include(t => t.VaiTro).OrderBy(t => t.Id).AsEnumerable()
            .Select(t => new { t.Id, t.TenDangNhap, t.HoTen, VaiTro = t.VaiTro?.TenVaiTro, HoatDong = t.HoatDong }).ToList();
    }

    private int? Chon() => grid.CurrentRow?.Cells["Id"].Value is int id ? id : null;

    private static string? Hoi(string tieuDe, string nhan, bool matKhau = false)
    {
        using var f = new Form { Text = tieuDe, Size = new Size(340, 150), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var lbl = new Label { Text = nhan, Left = 12, Top = 12, AutoSize = true };
        var txt = new TextBox { Left = 12, Top = 36, Width = 300, UseSystemPasswordChar = matKhau };
        var ok = new Button { Text = "OK", Left = 232, Top = 70, DialogResult = DialogResult.OK };
        f.Controls.AddRange(new Control[] { lbl, txt, ok }); f.AcceptButton = ok;
        return f.ShowDialog() == DialogResult.OK ? txt.Text : null;
    }

    private void Them()
    {
        var user = Hoi("Thêm tài khoản", "Tên đăng nhập:"); if (string.IsNullOrWhiteSpace(user)) return;
        var ten = Hoi("Thêm tài khoản", "Họ tên:"); if (string.IsNullOrWhiteSpace(ten)) return;
        var pass = Hoi("Thêm tài khoản", "Mật khẩu (tối thiểu 6 ký tự):", true);
        if (pass == null || pass.Length < 6) { MessageBox.Show("Mật khẩu phải có ít nhất 6 ký tự."); return; }
        var admin = MessageBox.Show("Cấp quyền Admin cho tài khoản này?\n(Chọn No để tạo nhân viên thường)", "Phân quyền", MessageBoxButtons.YesNo) == DialogResult.Yes;
        try
        {
            using var db = Program.NewDb();
            if (db.TaiKhoans.Any(t => t.TenDangNhap == user.Trim())) { MessageBox.Show("Tên đăng nhập đã tồn tại."); return; }
            var roleName = admin ? "Admin" : "NhanVien";
            var role = db.VaiTros.First(v => v.TenVaiTro == roleName);
            db.TaiKhoans.Add(new TaiKhoan { TenDangNhap = user.Trim(), HoTen = ten.Trim(), MatKhauHash = PasswordHasher.Hash(pass), VaiTroId = role.Id });
            db.SaveChanges(); Tai();
        }
        catch (Exception ex) { MessageBox.Show(ex.GetBaseException().Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void DatLaiMatKhau()
    {
        if (Chon() is not int id) return;
        var pass = Hoi("Đặt lại mật khẩu", "Mật khẩu mới (tối thiểu 6 ký tự):", true);
        if (pass == null) return;
        if (pass.Length < 6) { MessageBox.Show("Mật khẩu phải có ít nhất 6 ký tự."); return; }
        using var db = Program.NewDb();
        var tk = db.TaiKhoans.Find(id); if (tk == null) return;
        tk.MatKhauHash = PasswordHasher.Hash(pass); db.SaveChanges();
        MessageBox.Show("Đã đặt lại mật khẩu.");
    }

    private void Khoa()
    {
        if (Chon() is not int id) return;
        if (id == _currentUserId) { MessageBox.Show("Không thể tự khoá tài khoản đang đăng nhập."); return; }
        using var db = Program.NewDb();
        var tk = db.TaiKhoans.Find(id); if (tk == null) return;
        tk.HoatDong = !tk.HoatDong; db.SaveChanges(); Tai();
    }
}
