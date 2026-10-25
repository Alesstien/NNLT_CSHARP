//MainForm.cs: Cửa sổ Giao diện chính sau khi đăng nhập, đóng vai trò là khung chứa các Panel chức năng khác.
using Microsoft.EntityFrameworkCore;
using QLNH.Data;
using QLNH.Data.Entities;

namespace QLNH.Admin;

public class MainForm : Form
{
    public bool DangXuat { get; private set; }

    private sealed class BanLookup
    {
        public int? Id { get; set; }
        public string TenBan { get; set; } = "";
    }

    public MainForm(TaiKhoan user)
    {
        bool isAdmin = user.VaiTro?.TenVaiTro == "Admin";
        Text = $"Quản lý nhà hàng - {user.HoTen} ({user.VaiTro?.TenVaiTro})";
        WindowState = FormWindowState.Maximized; MinimumSize = new Size(900, 600);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        var pnlOrders = new OrdersPanel();
        var pnlPos = new PosPanel();
        var pnlDatBan = new CrudPanel<DatBan>(q => q.DatBans.OrderByDescending(d => d.Id), CauHinhDatBan);

        // Phân quyền: nhân viên thấy Đơn hàng, POS & Đặt bàn; Admin thấy toàn bộ
        AddTab(tabs, "🧾 Đơn hàng", pnlOrders);
        AddTab(tabs, "🛒 Bán tại bàn (POS)", pnlPos);
        AddTab(tabs, "📅 Đặt bàn", pnlDatBan);

        CrudPanel<MonAn>? pnlMonAn = null;
        CrudPanel<DanhMuc>? pnlDanhMuc = null;
        CrudPanel<Ban>? pnlBan = null;
        ReportPanel? pnlReport = null;
        AccountPanel? pnlAccount = null;

        if (isAdmin)
        {
            pnlMonAn = new CrudPanel<MonAn>(q => q.MonAns.OrderBy(m => m.DanhMucId).ThenBy(m => m.Id), CauHinhMonAn);
            pnlDanhMuc = new CrudPanel<DanhMuc>(q => q.DanhMucs.OrderBy(d => d.TenDanhMuc));
            pnlBan = new CrudPanel<Ban>(q => q.Bans.OrderBy(b => b.TenBan), CauHinhBan);
            pnlReport = new ReportPanel();
            pnlAccount = new AccountPanel(user.Id);

            AddTab(tabs, "🍽 Món ăn", pnlMonAn);
            AddTab(tabs, "📂 Danh mục", pnlDanhMuc);
            AddTab(tabs, "🪑 Bàn", pnlBan);
            AddTab(tabs, "📊 Doanh thu", pnlReport);
            AddTab(tabs, "👤 Tài khoản", pnlAccount);
        }

        // Tự động làm mới dữ liệu khi chuyển tab để luôn đồng bộ với Web và các tab khác
        tabs.SelectedIndexChanged += (_, _) =>
        {
            var ctrl = tabs.SelectedTab?.Controls.Count > 0 ? tabs.SelectedTab.Controls[0] : null;
            if (ctrl == pnlOrders) pnlOrders.Tai();
            else if (ctrl == pnlPos) pnlPos.LoadDataFromDb();
            else if (ctrl == pnlDatBan) pnlDatBan.Tai();
            else if (ctrl == pnlMonAn) pnlMonAn?.Tai();
            else if (ctrl == pnlDanhMuc) pnlDanhMuc?.Tai();
            else if (ctrl == pnlBan) pnlBan?.Tai();
            else if (ctrl == pnlReport) pnlReport?.Xem();
            else if (ctrl == pnlAccount) pnlAccount?.Tai();
        };

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

    private static void ThayCot(DataGridView g, string name, DataGridViewComboBoxColumn cotMoi)
    {
        if (g.Columns[name] is DataGridViewComboBoxColumn existing)
        {
            existing.DataSource = cotMoi.DataSource;
            existing.DisplayMember = cotMoi.DisplayMember;
            existing.ValueMember = cotMoi.ValueMember;
            return;
        }
        int idx = g.Columns[name]?.Index ?? -1;
        if (idx < 0) return;
        cotMoi.DataPropertyName = name; cotMoi.Name = name; cotMoi.HeaderText = name;
        g.Columns.RemoveAt(idx); g.Columns.Insert(idx, cotMoi);
    }

    private static DataGridViewComboBoxColumn Combo(object nguon, string hien, string tri) =>
        new() { DataSource = nguon, DisplayMember = hien, ValueMember = tri, FlatStyle = FlatStyle.Flat, Width = 140 };

    private static void CauHinhMonAn(DataGridView g, AppDbContext db) =>
        ThayCot(g, "DanhMucId", Combo(db.DanhMucs.AsNoTracking().OrderBy(d => d.TenDanhMuc).ToList(), "TenDanhMuc", "Id"));

    private static void CauHinhBan(DataGridView g, AppDbContext db) =>
        ThayCot(g, "TrangThai", Combo(new[] { "Trống", "Đang dùng", "Đã đặt", "Bảo trì" }, "", ""));

    private static void CauHinhDatBan(DataGridView g, AppDbContext db)
    {
        var trangThai = Combo(new[] { "Chờ xác nhận", "Đã xác nhận", "Đã đến", "Đã hủy" }, "", "");
        ThayCot(g, "TrangThai", trangThai);
        var bans = db.Bans.AsNoTracking().OrderBy(b => b.TenBan)
            .Select(b => new BanLookup { Id = b.Id, TenBan = b.TenBan })
            .ToList();
        bans.Insert(0, new BanLookup { Id = null, TenBan = "(Chưa xếp bàn)" });
        ThayCot(g, "BanId", Combo(bans, "TenBan", "Id"));
    }
}
