using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using QLNH.Data;
using QLNH.Data.Entities;

namespace QLNH.Admin;

public class CrudPanel<T> : UserControl where T : class, new()
{
    private readonly AppDbContext _db = Program.NewDb();
    private readonly BindingList<T> _list = new();
    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        AutoGenerateColumns = false,
        AllowUserToAddRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        BackgroundColor = Color.White
    };
    private readonly TextBox _txtSearch = new() { Width = 220, PlaceholderText = "Tìm kiếm..." };
    private readonly Func<AppDbContext, IQueryable<T>>? _query;
    private readonly Action<DataGridView, AppDbContext>? _configure;
    private readonly bool _canEdit;

    public CrudPanel(Func<AppDbContext, IQueryable<T>>? query = null, Action<DataGridView, AppDbContext>? configure = null, bool canEdit = true)
    {
        _query = query;
        _configure = configure;
        _canEdit = canEdit;
        Dock = DockStyle.Fill;

        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42, Padding = new Padding(6) };
        var btnAdd = Btn("Thêm dòng", Color.SeaGreen, (_, _) => Them());
        var btnSave = Btn("Lưu thay đổi", Color.SteelBlue, (_, _) => Luu());
        var btnDel = Btn("Xóa", Color.Firebrick, (_, _) => Xoa());
        if (typeof(T) == typeof(WebUuDai))
            btnDel.Visible = false;
        var btnReload = Btn("Tải lại", Color.Gray, (_, _) => Tai());
        _txtSearch.TextChanged += (_, _) => Loc();

        if (typeof(T) == typeof(MonAn))
        {
            var btnImg = Btn("Chọn ảnh...", Color.DarkOrange, (_, _) => ChonAnhMon());
            bar.Controls.AddRange(new Control[] { btnAdd, btnImg, btnSave, btnDel, btnReload, _txtSearch });
        }
        else
        {
            bar.Controls.AddRange(new Control[] { btnAdd, btnSave, btnDel, btnReload, _txtSearch });
        }

        if (!canEdit)
        {
            btnAdd.Visible = btnSave.Visible = btnDel.Visible = false;
            _grid.ReadOnly = true;
        }

        if (canEdit && typeof(T) == typeof(DatBan))
        {
            bar.Controls.Add(Btn("Bắt đầu phục vụ", Color.DarkOrange, (_, _) => BatDauPhucVu()));
        }

        BuildColumns();

        // Cấu hình riêng giao diện quản lý mã ưu đãi.
        // Các module CRUD khác giữ nguyên cách hiển thị ban đầu.
        if (typeof(T) == typeof(WebUuDai))
        {
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            foreach (DataGridViewColumn col in _grid.Columns)
            {
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                col.MinimumWidth = 65;
            }

            void CauHinhCot(string tenCot, string tieuDe, float tiLe, int doRongToiThieu)
            {
                if (_grid.Columns[tenCot] is not DataGridViewColumn col)
                    return;

                col.HeaderText = tieuDe;
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                col.FillWeight = tiLe;
                col.MinimumWidth = doRongToiThieu;
            }

            CauHinhCot(nameof(WebUuDai.Id), "ID", 45, 55);
            CauHinhCot(nameof(WebUuDai.Ma), "Mã ưu đãi", 100, 90);
            CauHinhCot(nameof(WebUuDai.MoTa), "Mô tả", 190, 160);
            CauHinhCot(nameof(WebUuDai.PhanTramGiam), "% giảm", 85, 75);
            CauHinhCot(nameof(WebUuDai.GiamToiDa), "Giảm tối đa", 110, 100);
            CauHinhCot(nameof(WebUuDai.HanDung), "Hạn dùng", 115, 105);
            CauHinhCot(nameof(WebUuDai.HoatDong), "Hoạt động", 90, 85);

            if (_grid.Columns[nameof(WebUuDai.HoatDong)] is DataGridViewColumn cotHoatDong)
