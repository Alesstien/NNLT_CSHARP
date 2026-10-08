//MainForm.cs: Cửa sổ Giao diện chính sau khi đăng nhập, đóng vai trò là khung chứa các Panel chức năng khác.
using Microsoft.EntityFrameworkCore;
using QLNH.Data;
using QLNH.Data.Entities;

namespace QLNH.Admin;

public class MainForm : Form
{
    public bool DangXuat { get; private set; }

    public MainForm(TaiKhoan user)
    {
        bool isAdmin = user.VaiTro?.TenVaiTro == "Admin";
        Text = $"Quản lý nhà hàng - {user.HoTen} ({user.VaiTro?.TenVaiTro})";
        WindowState = FormWindowState.Maximized; MinimumSize = new Size(900, 600);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        // Phân quyền: nhân viên chỉ thấy Đơn hàng & Đặt bàn
        AddTab(tabs, "🧾 Đơn hàng", new OrdersPanel());
        AddTab(tabs, "📅 Đặt bàn", new CrudPanel<DatBan>(q => q.DatBans.OrderByDescending(d => d.NgayGio), CauHinhDatBan));
        if (isAdmin)
        {
            AddTab(tabs, "🍽 Món ăn", new CrudPanel<MonAn>(q => q.MonAns.OrderBy(m => m.DanhMucId).ThenBy(m => m.TenMon), CauHinhMonAn));
            AddTab(tabs, "📂 Danh mục", new CrudPanel<DanhMuc>(q => q.DanhMucs.OrderBy(d => d.TenDanhMuc)));
            AddTab(tabs, "🪑 Bàn", new CrudPanel<Ban>(q => q.Bans.OrderBy(b => b.TenBan), CauHinhBan));
            AddTab(tabs, "📊 Doanh thu", new ReportPanel());
            AddTab(tabs, "👤 Tài khoản", new AccountPanel(user.Id));
        }

        var menu = new MenuStrip();
        var mnuHeThong = new ToolStripMenuItem("Hệ thống");
        mnuHeThong.DropDownItems.Add("Đăng xuất", null, (_, _) => { DangXuat = true; Close(); });
        mnuHeThong.DropDownItems.Add("Thoát", null, (_, _) => Close());
        var mnuTroGiup = new ToolStripMenuItem("Trợ giúp");
        mnuTroGiup.DropDownItems.Add("Giới thiệu", null, (_, _) => MessageBox.Show("Hệ thống quản lý nhà hàng\nĐồ án môn Ngôn ngữ lập trình C#\nC# WinForms + ASP.NET Core MVC + SQL Server + EF Core", "Giới thiệu"));
        menu.Items.AddRange(new ToolStripItem[] { mnuHeThong, mnuTroGiup });
        MainMenuStrip = menu;
        Controls.Add(tabs); Controls.Add(menu);
    }

    private static void AddTab(TabControl tabs, string title, Control c)
    {
        var page = new TabPage(title); page.Controls.Add(c); tabs.TabPages.Add(page);
    }

    private static void ThayCot(DataGridView g, string name, DataGridViewColumn cotMoi)
    {
        int idx = g.Columns[name]?.Index ?? -1;
        if (idx < 0) return;
        cotMoi.DataPropertyName = name; cotMoi.Name = name; cotMoi.HeaderText = name;
        g.Columns.RemoveAt(idx); g.Columns.Insert(idx, cotMoi);
    }

    private static DataGridViewComboBoxColumn Combo(object nguon, string hien, string tri) =>
        new() { DataSource = nguon, DisplayMember = hien, ValueMember = tri, FlatStyle = FlatStyle.Flat };

    private static void CauHinhMonAn(DataGridView g, AppDbContext db) =>
        ThayCot(g, "DanhMucId", Combo(db.DanhMucs.AsNoTracking().OrderBy(d => d.TenDanhMuc).ToList(), "TenDanhMuc", "Id"));

    private static void CauHinhBan(DataGridView g, AppDbContext db) =>
        ThayCot(g, "TrangThai", Combo(new[] { "Trống", "Đang dùng", "Đã đặt", "Bảo trì" }, "", ""));

    private static void CauHinhDatBan(DataGridView g, AppDbContext db)
    {
        var trangThai = Combo(new[] { "Chờ xác nhận", "Đã xác nhận", "Đã đến", "Đã huỷ" }, "", "");
        ThayCot(g, "TrangThai", trangThai);
        var bans = db.Bans.AsNoTracking().OrderBy(b => b.TenBan).ToList();
        ThayCot(g, "BanId", Combo(bans, "TenBan", "Id"));
    }
}
