//CrudPanel.cs: Giao diện quản lý danh mục và món ăn (thêm món mới, cập nhật giá, sửa mô tả, xóa món).
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using QLNH.Data;

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
        var btnAdd = Btn("＋ Thêm", Color.SeaGreen, (_, _) => Them());
        var btnSave = Btn("💾 Lưu", Color.SteelBlue, (_, _) => Luu());
        var btnDel = Btn("✖ Xoá", Color.Firebrick, (_, _) => Xoa());
        var btnReload = Btn("⟳ Tải lại", Color.Gray, (_, _) => Tai());
        _txtSearch.TextChanged += (_, _) => Loc();
        bar.Controls.AddRange(new Control[] { btnAdd, btnSave, btnDel, btnReload, _txtSearch });
        if (!canEdit) { btnAdd.Visible = btnSave.Visible = btnDel.Visible = false; _grid.ReadOnly = true; }

        BuildColumns();
        _grid.DataError += (_, e) => { MessageBox.Show("Giá trị không hợp lệ: " + e.Exception?.Message, "Lỗi dữ liệu"); e.ThrowException = false; };
        Controls.Add(_grid); Controls.Add(bar);
        Tai();
    }

    private static Button Btn(string text, Color c, EventHandler h)
    {
        var b = new Button { Text = text, AutoSize = true, BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
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
            foreach (var e in _db.ChangeTracker.Entries().ToList()) e.State = EntityState.Detached;
            var q = _query != null ? _query(_db) : _db.Set<T>();
            _list.Clear();
            foreach (var x in q.AsNoTracking().ToList()) { _db.Attach(x); _list.Add(x); }
            _txtSearch.Clear();
        }
        catch (Exception ex) { MessageBox.Show(ex.GetBaseException().Message, "Lỗi tải dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void Them()
    {
        var item = new T(); _db.Add(item); _list.Add(item);
        _grid.ClearSelection();
        _grid.CurrentCell = _grid.Rows[^1].Cells.Cast<DataGridViewCell>().First(c => c.Visible);
        _grid.BeginEdit(true);
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
        try { _db.SaveChanges(); MessageBox.Show("Đã lưu thay đổi.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information); Tai(); }
        catch (DbUpdateException ex) { MessageBox.Show("Không lưu được (trùng dữ liệu hoặc vi phạm ràng buộc):\n" + ex.GetBaseException().Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        catch (Exception ex) { MessageBox.Show(ex.GetBaseException().Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void Xoa()
    {
        if (_grid.CurrentRow?.DataBoundItem is not T item) return;
        if (MessageBox.Show("Bạn có chắc muốn xoá dòng đã chọn?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try { _db.Remove(item); _db.SaveChanges(); Tai(); }
        catch (DbUpdateException ex)
        {
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
