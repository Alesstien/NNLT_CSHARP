//CrudPanel.cs: Giao diện quản lý danh mục và món ăn (thêm món mới, cập nhật giá, sửa mô tả, xóa món).
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using QLNH.Data;
using QLNH.Data.Entities;

namespace QLNH.Admin;

/// <summary>Panel CRUD dùng chung (Generic): hiển thị, thêm, sửa, xoá, tìm kiếm cho một thực thể bất kỳ.</summary>
public class CrudPanel<T> : UserControl where T : class, new()
{
    private readonly AppDbContext _db = Program.NewDb();
    private readonly BindingList<T> _list = new();
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, AutoGenerateColumns = false, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, BackgroundColor = Color.White };
    private readonly TextBox _txtSearch = new() { Width = 220, PlaceholderText = "Tìm kiếm..." };
    private readonly Func<AppDbContext, IQueryable<T>>? _query;
    private readonly Action<DataGridView, AppDbContext>? _configure;
    private readonly bool _canEdit;

    public CrudPanel(Func<AppDbContext, IQueryable<T>>? query = null, Action<DataGridView, AppDbContext>? configure = null, bool canEdit = true)
    {
        _query = query; _configure = configure; _canEdit = canEdit;
        Dock = DockStyle.Fill;

        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42, Padding = new Padding(6) };
        var btnAdd = Btn("＋ Thêm dòng", Color.SeaGreen, (_, _) => Them());
        var btnSave = Btn("💾 Lưu thay đổi", Color.SteelBlue, (_, _) => Luu());
        var btnDel = Btn("✖ Xoá", Color.Firebrick, (_, _) => Xoa());
        var btnReload = Btn("⟳ Tải lại", Color.Gray, (_, _) => Tai());
        _txtSearch.TextChanged += (_, _) => Loc();

        if (typeof(T) == typeof(MonAn))
        {
            var btnImg = Btn("🖼 Chọn ảnh...", Color.DarkOrange, (_, _) => ChonAnhMon());
            bar.Controls.AddRange(new Control[] { btnAdd, btnImg, btnSave, btnDel, btnReload, _txtSearch });
        }
        else
        {
            bar.Controls.AddRange(new Control[] { btnAdd, btnSave, btnDel, btnReload, _txtSearch });
        }

        if (!canEdit) { btnAdd.Visible = btnSave.Visible = btnDel.Visible = false; _grid.ReadOnly = true; }

        BuildColumns();
        _grid.DataError += (_, e) => { e.ThrowException = false; };
        Controls.Add(_grid); Controls.Add(bar);
        Tai();
    }

    private static Button Btn(string text, Color c, EventHandler h)
    {
        var b = new Button { Text = text, AutoSize = true, BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
        b.Click += h; return b;
    }

    // Chỉ sinh cột cho thuộc tính đơn giản (bỏ navigation property và thuộc tính chỉ đọc)
    private void BuildColumns()
    {
        foreach (var p in typeof(T).GetProperties())
        {
            var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
            bool simple = t.IsPrimitive || t == typeof(string) || t == typeof(decimal) || t == typeof(DateTime);
            if (!simple || !p.CanWrite) continue;
            DataGridViewColumn col = t == typeof(bool) ? new DataGridViewCheckBoxColumn() : new DataGridViewTextBoxColumn();
            col.DataPropertyName = p.Name; col.Name = p.Name; col.HeaderText = p.Name;
            col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            if (p.Name == "Id") col.ReadOnly = true;
            if (t == typeof(decimal)) col.DefaultCellStyle.Format = "N0";
            _grid.Columns.Add(col);
        }
        _configure?.Invoke(_grid, _db);
        _grid.DataSource = _list;
    }

    public void Tai()
    {
        try
        {
            _grid.EndEdit();
            foreach (var e in _db.ChangeTracker.Entries().ToList()) e.State = EntityState.Detached;
            _configure?.Invoke(_grid, _db);
            var q = _query != null ? _query(_db) : _db.Set<T>();
            _list.Clear();
            foreach (var x in q.AsNoTracking().ToList())
            {
                if (x is DatBan dbItem && dbItem.TrangThai == "Đã huỷ")
                {
                    dbItem.TrangThai = "Đã hủy";
                }
                _db.Attach(x);
                _list.Add(x);
            }
            _txtSearch.Clear();
        }
        catch (Exception ex) { MessageBox.Show(ex.GetBaseException().Message, "Lỗi tải dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void Them()
    {
        _configure?.Invoke(_grid, _db);
        var item = new T();
        if (item is MonAn mon)
        {
            var dmId = _db.DanhMucs.AsNoTracking().OrderBy(d => d.Id).Select(d => d.Id).FirstOrDefault();
            if (dmId == 0)
            {
                var dm = new DanhMuc { TenDanhMuc = "Món chính", MoTa = "Các món ăn chính" };
                _db.DanhMucs.Add(dm);
                _db.SaveChanges();
                _configure?.Invoke(_grid, _db);
                dmId = dm.Id;
            }
            mon.DanhMucId = dmId;
            mon.Gia = 50000;
            mon.HinhAnh = "/images/com-tam.jpg";
            mon.ConPhucVu = true;
        }
        else if (item is DatBan dbItem)
        {
            dbItem.NgayGio = DateTime.Now.AddHours(2);
            dbItem.SoNguoi = 2;
            dbItem.BanId = _db.Bans.AsNoTracking().OrderBy(b => b.Id).Select(b => (int?)b.Id).FirstOrDefault();
            dbItem.TrangThai = "Chờ xác nhận";
        }
        else if (item is Ban ban)
        {
            ban.SoGhe = 4;
            ban.TrangThai = "Trống";
        }

        _db.Add(item);
        _list.Add(item);
        _grid.ClearSelection();
        var editableCell = _grid.Rows[^1].Cells.Cast<DataGridViewCell>().FirstOrDefault(c => c.Visible && !c.ReadOnly)
                           ?? _grid.Rows[^1].Cells.Cast<DataGridViewCell>().First(c => c.Visible);
        _grid.CurrentCell = editableCell;
        _grid.BeginEdit(true);
    }

    private void ChonAnhMon()
    {
        if (_grid.CurrentRow?.DataBoundItem is not MonAn mon)
        {
            MessageBox.Show("Hãy chọn một dòng món ăn trước khi chọn ảnh.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new OpenFileDialog
        {
            Title = "Chọn ảnh cho món ăn",
            Filter = "Tệp hình ảnh (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp|Tất cả tệp (*.*)|*.*"
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        try
        {
            var fileName = Path.GetFileName(dlg.FileName).Replace(" ", "-").ToLowerInvariant();
            var webImgDir = TimThuMucAnhWeb();
            if (webImgDir != null)
            {
                Directory.CreateDirectory(webImgDir);
                var destPath = Path.Combine(webImgDir, fileName);
                if (!string.Equals(Path.GetFullPath(dlg.FileName), Path.GetFullPath(destPath), StringComparison.OrdinalIgnoreCase))
                    File.Copy(dlg.FileName, destPath, overwrite: true);
            }
            mon.HinhAnh = "/images/" + fileName;
            _grid.Refresh();
            MessageBox.Show($"Đã gán ảnh '{mon.HinhAnh}'. Hãy bấm '💾 Lưu thay đổi' để cập nhật lên Web.", "Chọn ảnh thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể sao chép ảnh: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string? TimThuMucAnhWeb()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "QLNH.Web", "wwwroot", "images");
            if (Directory.Exists(Path.Combine(dir.FullName, "QLNH.Web"))) return candidate;
            dir = dir.Parent;
        }
        return @"D:\QuanLyNhaHang\QuanLyNhaHang\QLNH.Web\wwwroot\images";
    }

    private void Luu()
    {
        _grid.EndEdit();
        foreach (var x in _list)
        {
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(x, new ValidationContext(x), results, true))
            {
                MessageBox.Show(string.Join("\n", results.Select(r => "• " + r.ErrorMessage)), "Dữ liệu chưa hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }
        try
        {
            _db.SaveChanges();

            if (typeof(T) == typeof(MonAn) || typeof(T) == typeof(DanhMuc))
            {
                DongBoMonAnSangWeb();
            }
            else if (typeof(T) == typeof(DatBan))
            {
                DongBoDatBanSangWeb();
            }

            MessageBox.Show("Đã lưu thay đổi và đồng bộ lên Web thành công.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Tai();
        }
        catch (DbUpdateException ex) { MessageBox.Show("Không lưu được (trùng dữ liệu hoặc vi phạm ràng buộc):\n" + ex.GetBaseException().Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        catch (Exception ex) { MessageBox.Show(ex.GetBaseException().Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void DongBoMonAnSangWeb()
    {
        try
        {
            var mons = _db.MonAns.Include(m => m.DanhMuc).AsNoTracking().ToList();
            foreach (var m in mons)
            {
                var tenDm = m.DanhMuc?.TenDanhMuc ?? "Món chính";
                var moTa = m.MoTa ?? "";
                var hinhAnh = string.IsNullOrWhiteSpace(m.HinhAnh) ? "/images/com-tam.jpg" : m.HinhAnh;
                _db.Database.ExecuteSqlRaw(@"
                    IF OBJECT_ID('Web_MucThucDon', 'U') IS NOT NULL
                    BEGIN
                        IF EXISTS (SELECT 1 FROM Web_MucThucDon WHERE Ten = {0})
                            UPDATE Web_MucThucDon SET Gia = {1}, MoTa = {2}, DanhMuc = {3}, HinhAnh = {4}, ConPhucVu = {5} WHERE Ten = {0};
                        ELSE
                            INSERT INTO Web_MucThucDon (Ten, MoTa, Gia, DanhMuc, HinhAnh, Emoji, ThuTu, ConPhucVu)
                            VALUES ({0}, {2}, {1}, {3}, {4}, N'🍽️', {6}, {5});
                    END",
                    m.TenMon, m.Gia, moTa, tenDm, hinhAnh, m.ConPhucVu, m.Id + 10);
            }
        }
        catch { /* bỏ qua nếu bảng Web_MucThucDon chưa tạo */ }
    }

    private void DongBoDatBanSangWeb()
    {
        foreach (var item in _list.OfType<DatBan>())
        {
            string? ma = null;
            if (!string.IsNullOrEmpty(item.GhiChu) && item.GhiChu.StartsWith('['))
            {
                var idx = item.GhiChu.IndexOf(']');
                if (idx > 1) ma = item.GhiChu[1..idx].Trim();
            }

            var ttWeb = item.TrangThai switch
            {
                "Đã xác nhận" => "Đã xác nhận",
                "Đã đến" => "Đã hoàn thành",
                "Đã huỷ" or "Đã hủy" => "Đã hủy",
                _ => "Đang chờ duyệt"
            };
            var ttDon = item.TrangThai switch
            {
                "Đã xác nhận" => TrangThaiDon.DangCheBien,
                "Đã đến" => TrangThaiDon.DaThanhToan,
                "Đã huỷ" or "Đã hủy" => TrangThaiDon.DaHuy,
                _ => TrangThaiDon.Moi
            };

            if (!string.IsNullOrEmpty(ma))
            {
                var don = _db.DonHangs.FirstOrDefault(d => d.GhiChu != null && d.GhiChu.StartsWith("[" + ma + "]"));
                if (don != null && don.TrangThai != ttDon)
                {
                    don.TrangThai = ttDon;
                    _db.SaveChanges();
                }
                try
                {
                    _db.Database.ExecuteSqlRaw(
                        "IF OBJECT_ID('Web_DatBan', 'U') IS NOT NULL UPDATE Web_DatBan SET HoTen = {0}, SoDienThoai = {1}, NgayGio = {2}, SoKhach = {3}, TrangThai = {4}, NgayCapNhat = GETDATE() WHERE MaDatBan = {5}",
                        item.TenKhach, item.SoDienThoai, item.NgayGio, item.SoNguoi, ttWeb, ma);
                }
                catch { }
            }
            else
            {
                // Nếu đặt bàn được tạo mới trực tiếp từ WinForms, gán mã [WF-...] và đẩy lên Web_DatBan để Web Admin cũng nhìn thấy
                ma = $"WF-{100000 + item.Id}";
                item.GhiChu = string.IsNullOrWhiteSpace(item.GhiChu) ? $"[{ma}] Tạo từ WinForms" : $"[{ma}] {item.GhiChu}";
                _db.SaveChanges();
                try
                {
                    _db.Database.ExecuteSqlRaw(@"
                        IF OBJECT_ID('Web_DatBan', 'U') IS NOT NULL AND EXISTS (SELECT 1 FROM Web_KhuVuc)
                        AND NOT EXISTS (SELECT 1 FROM Web_DatBan WHERE MaDatBan = {0})
                        BEGIN
                            DECLARE @kvId INT = (SELECT TOP 1 Id FROM Web_KhuVuc ORDER BY Id);
                            INSERT INTO Web_DatBan (MaDatBan, HoTen, SoDienThoai, Email, NgayGio, SoKhach, SoBan, KhuVucId, GhiChu, TongMon, PhuThuKhuVuc, Vat, PhiPhucVu, GiamGia, TongHoaDon, CheDoCoc, TienCoc, ConLai, PhuongThucThanhToan, MaGiaoDich, DaThanhToanCoc, TrangThai, NgayTao)
                            VALUES ({0}, {1}, {2}, N'taiquay@huongviet.vn', {3}, {4}, 1, @kvId, {5}, 0, 0, 0, 0, 0, 0, N'CocCoDinh', 0, 0, N'TAI_QUAY', {0}, 1, {6}, GETDATE());
                        END",
                        ma, item.TenKhach, item.SoDienThoai, item.NgayGio, item.SoNguoi, item.GhiChu, ttWeb);
                }
                catch { }
            }
        }
    }

    private void Xoa()
    {
        if (_grid.CurrentRow?.DataBoundItem is not T item) return;

        // Nếu dòng mới thêm chưa lưu vào DB thì chỉ cần gỡ khỏi lưới
        if (_db.Entry(item).State == EntityState.Added)
        {
            _db.Entry(item).State = EntityState.Detached;
            _list.Remove(item);
            return;
        }

        if (MessageBox.Show("Bạn có chắc muốn xoá dòng đã chọn?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            string? tenMonXoa = (item as MonAn)?.TenMon;
            _db.Remove(item);
            _db.SaveChanges();

            if (!string.IsNullOrEmpty(tenMonXoa))
            {
                try
                {
                    _db.Database.ExecuteSqlRaw("IF OBJECT_ID('Web_MucThucDon', 'U') IS NOT NULL DELETE FROM Web_MucThucDon WHERE Ten = {0} AND NOT EXISTS (SELECT 1 FROM Web_DatBanMon m JOIN Web_MucThucDon t ON m.MucThucDonId = t.Id WHERE t.Ten = {0})", tenMonXoa);
                }
                catch { }
            }
            Tai();
        }
        catch (DbUpdateException ex)
        {
            if (item is MonAn mon)
            {
                _db.Entry(mon).State = EntityState.Unchanged;
                if (MessageBox.Show("Món ăn này đã có trong lịch sử đơn hàng nên không thể xóa vĩnh viễn.\nBạn có muốn chuyển sang 'Ngừng phục vụ' để ẩn khỏi thực đơn Web không?", "Gợi ý", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    mon.ConPhucVu = false;
                    _db.SaveChanges();
                    DongBoMonAnSangWeb();
                    Tai();
                    return;
                }
            }
            MessageBox.Show("Không thể xoá vì dữ liệu đang được sử dụng ở nơi khác.\n" + ex.GetBaseException().Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Tai();
        }
    }

    private void Loc()
    {
        var kw = _txtSearch.Text.Trim().ToLowerInvariant();
        CurrencyManager? cm = (CurrencyManager?)BindingContext?[_list];
        cm?.SuspendBinding();
        foreach (DataGridViewRow r in _grid.Rows)
        {
            bool match = kw.Length == 0 || r.Cells.Cast<DataGridViewCell>().Any(c => (c.Value?.ToString() ?? "").ToLowerInvariant().Contains(kw));
            r.Visible = match;
        }
        cm?.ResumeBinding();
    }
}
