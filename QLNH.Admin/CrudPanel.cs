using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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
    private readonly TextBox _txtSearch = new()
    {
        Width = 200,
        PlaceholderText = "Tìm kiếm...",
        Margin = new Padding(0, 5, 8, 6)
    };    
    private readonly Func<AppDbContext, IQueryable<T>>? _query;
    private readonly Action<DataGridView, AppDbContext>? _configure;
    private readonly bool _canEdit;

    //Thêm biến chặn sự kiện và quyền xác nhận cọc
    private bool _dangTai;
    private bool _dangXuLyTrangThai;
    private readonly bool _duocXacNhanCoc;

    private sealed class ThongTinCocWeb
    {
        public decimal TienCoc { get; init; }
        public string MaGiaoDich { get; init; } = "";
        public bool DaThanhToanCoc { get; init; }
        public string TrangThai { get; init; } = "";
    }

    private void CanChinhGiaoDienDatBan()
{
    if (typeof(T) != typeof(DatBan))
    {
        return;
    }

    _grid.BorderStyle = BorderStyle.None;
    _grid.BackgroundColor = Color.White;

    _grid.RowHeadersVisible = false;
    _grid.AllowUserToResizeRows = false;
    _grid.AutoSizeColumnsMode =
        DataGridViewAutoSizeColumnsMode.Fill;

    _grid.ColumnHeadersHeightSizeMode =
        DataGridViewColumnHeadersHeightSizeMode.AutoSize;

    _grid.ColumnHeadersDefaultCellStyle.WrapMode =
        DataGridViewTriState.True;

    _grid.DefaultCellStyle.Padding =
        new Padding(4, 3, 4, 3);

    _grid.AutoSizeRowsMode =
        DataGridViewAutoSizeRowsMode.DisplayedCells;

    _grid.AlternatingRowsDefaultCellStyle.BackColor =
        Color.FromArgb(245, 247, 250);

    void ChinhCot(
        string ten,
        string tieuDe,
        float tiLe,
        int rongToiThieu)
    {
        var cot = _grid.Columns[ten];

        if (cot == null)
        {
            return;
        }

        cot.HeaderText = tieuDe;

        // Ghi đè AllCells và độ rộng của cột ComboBox.
        cot.AutoSizeMode =
            DataGridViewAutoSizeColumnMode.Fill;

        cot.FillWeight = tiLe;
        cot.MinimumWidth = rongToiThieu;
    }

    ChinhCot("MaDatBan", "Mã đặt bàn", 100, 110);
    ChinhCot("TenKhach", "Tên khách", 140, 140);
    ChinhCot("SoDienThoai", "Điện thoại", 100, 110);
    ChinhCot("NgayGio", "Ngày giờ", 125, 145);
    ChinhCot("BatDauPhucVuLuc", "Bắt đầu phục vụ", 115, 135);
    ChinhCot("SoNguoi", "Số người", 60, 65);
    ChinhCot("BanId", "Bàn", 80, 90);
    ChinhCot("TrangThai", "Trạng thái", 115, 135);
    ChinhCot("GhiChu", "Ghi chú", 200, 180);

    var cotId = _grid.Columns["Id"];

    if (cotId != null)
    {
        cotId.Visible = false;
    }

    var cotNgayGio = _grid.Columns["NgayGio"];

    if (cotNgayGio != null)
    {
        cotNgayGio.DefaultCellStyle.Format =
            "dd/MM/yyyy HH:mm";
    }

    var cotBatDau = _grid.Columns["BatDauPhucVuLuc"];

    if (cotBatDau != null)
    {
        cotBatDau.DefaultCellStyle.Format =
            "dd/MM/yyyy HH:mm";
    }

    var cotGhiChu = _grid.Columns["GhiChu"];

    if (cotGhiChu != null)
    {
        cotGhiChu.DefaultCellStyle.WrapMode =
            DataGridViewTriState.True;
    }
    }
    public CrudPanel(
        Func<AppDbContext, IQueryable<T>>? query = null,
        Action<DataGridView, AppDbContext>? configure = null,
        bool canEdit = true,
        bool canVerifyDeposit = false)
    {
        _query = query;
        _configure = configure;
        _canEdit = canEdit;
        _duocXacNhanCoc = canVerifyDeposit;
        Dock = DockStyle.Fill;

        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(8),
            Margin = new Padding(0),
            BackColor = Color.WhiteSmoke
        };        var btnAdd = Btn("Thêm dòng", Color.SeaGreen, (_, _) => Them());
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
            if (canVerifyDeposit)
            {
                bar.Controls.Add(Btn("Xác nhận cọc", Color.SeaGreen, (_, _) => XacNhanCoc()));
            }

            bar.Controls.Add(Btn("Bắt đầu phục vụ", Color.DarkOrange, (_, _) => BatDauPhucVu()));
        }

        BuildColumns();

        // Cấu hình riêng giao diện quản lý mã ưu đãi.
        // Chỉ áp dụng cho WebUuDai, không ảnh hưởng các bảng CRUD khác.
        if (typeof(T) == typeof(WebUuDai))
        {
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            foreach (DataGridViewColumn col in _grid.Columns)
            {
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                col.MinimumWidth = 65;
            }

            DataGridViewColumn? cot;

            cot = _grid.Columns[nameof(WebUuDai.Id)];
            if (cot != null)
            {
                cot.HeaderText = "ID";
                cot.FillWeight = 45;
                cot.MinimumWidth = 55;
            }

            cot = _grid.Columns[nameof(WebUuDai.Ma)];
            if (cot != null)
            {
                cot.HeaderText = "Mã ưu đãi";
                cot.FillWeight = 100;
                cot.MinimumWidth = 90;
            }

            cot = _grid.Columns[nameof(WebUuDai.MoTa)];
            if (cot != null)
            {
                cot.HeaderText = "Mô tả";
                cot.FillWeight = 190;
                cot.MinimumWidth = 160;
            }

            cot = _grid.Columns[nameof(WebUuDai.PhanTramGiam)];
            if (cot != null)
            {
                cot.HeaderText = "% giảm";
                cot.FillWeight = 85;
                cot.MinimumWidth = 75;
            }

            cot = _grid.Columns[nameof(WebUuDai.GiamToiDa)];
            if (cot != null)
            {
                cot.HeaderText = "Giảm tối đa";
                cot.FillWeight = 110;
                cot.MinimumWidth = 100;
            }

            cot = _grid.Columns[nameof(WebUuDai.HanDung)];
            if (cot != null)
            {
                cot.HeaderText = "Hạn dùng";
                cot.FillWeight = 115;
                cot.MinimumWidth = 105;
            }

            cot = _grid.Columns[nameof(WebUuDai.HoatDong)];
            if (cot != null)
            {
                cot.HeaderText = "Hoạt động";
                cot.FillWeight = 90;
                cot.MinimumWidth = 85;
                cot.DefaultCellStyle.Alignment =
                    DataGridViewContentAlignment.MiddleCenter;
            }

            _grid.RowTemplate.Height = 32;
            _grid.ColumnHeadersHeight = 38;
            _grid.ColumnHeadersHeightSizeMode =
                DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        }

        if (typeof(T) == typeof(DatBan))
        {
            _grid.Columns.Insert(0, new DataGridViewTextBoxColumn
            {
                Name = "STT",
                HeaderText = "STT",
                Width = 55,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            _grid.CellFormatting += (_, e) =>
            {
                if (e.RowIndex < 0 || _grid.Columns[e.ColumnIndex].Name != "STT")
                    return;

                e.Value = (e.RowIndex + 1).ToString();
                e.FormattingApplied = true;
            };
        }

        _grid.DataError += (_, e) => { e.ThrowException = false; };
        _grid.CellValidating += Grid_CellValidating;
        _grid.CellBeginEdit += Grid_CellBeginEdit;
        _grid.CurrentCellDirtyStateChanged += Grid_CurrentCellDirtyStateChanged;
        _grid.CellValueChanged += Grid_CellValueChanged;

        Controls.Add(_grid);
        Controls.Add(bar);
        Tai();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _db.Dispose();

        base.Dispose(disposing);
    }

    private void Grid_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        if (_grid.IsCurrentCellDirty && (_grid.CurrentCell is DataGridViewCheckBoxCell || _grid.CurrentCell is DataGridViewComboBoxCell))
        {
            _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }

        private void Grid_CellValueChanged(
        object? sender,
        DataGridViewCellEventArgs e)
    {
        if (_dangTai
            || _dangXuLyTrangThai
            || e.RowIndex < 0
            || e.ColumnIndex < 0)
        {
            return;
        }

        var tenCot = _grid.Columns[e.ColumnIndex].Name;

        // Giữ chức năng cập nhật món ăn.
        if (tenCot == "ConPhucVu"
            && _grid.Rows[e.RowIndex].DataBoundItem is MonAn mon
            && mon.Id > 0
            && !string.IsNullOrWhiteSpace(mon.TenMon)
            && mon.Gia >= 0)
        {
            try
            {
                _grid.EndEdit();
                _db.SaveChanges();
                DongBoMonAnSangWeb();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.GetBaseException().Message,
                    "Lỗi cập nhật món ăn");
            }

            return;
        }

        if (tenCot != nameof(DatBan.TrangThai)
            || _grid.Rows[e.RowIndex].DataBoundItem
                is not DatBan phieu
            || phieu.Id <= 0)
        {
            return;
        }

        var trangThaiMoi = _grid.Rows[e.RowIndex]
            .Cells[e.ColumnIndex].Value?.ToString()
            ?? "Chờ xác nhận";

        var trangThaiCu = _db.Entry(phieu)
            .Property(x => x.TrangThai).OriginalValue;

        if (trangThaiMoi == trangThaiCu)
        {
            return;
        }

        _dangXuLyTrangThai = true;

        BeginInvoke(new Action(() =>
        {
            try
            {
                _grid.EndEdit();
                (BindingContext?[_list] as CurrencyManager)
                    ?.EndCurrentEdit();

// Trả entity về giá trị cũ trước khi hỏi Yes/No.
phieu.TrangThai = trangThaiCu;

                // Trả entity về giá trị cũ trước khi hỏi Yes/No.
                phieu.TrangThai = trangThaiCu;

                if (trangThaiMoi == "Đã xác nhận")
                {
                    DuyetVaXacNhanCoc(phieu);
                }
                else if (trangThaiMoi == "Đã đến")
                {
                    MessageBox.Show(
                        "Hãy dùng nút 'Bắt đầu phục vụ' " +
                        "để tiếp nhận khách.",
                        "Thông báo");
                }
                else
                {
                    LuuTrangThaiDatBan(phieu, trangThaiMoi);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.GetBaseException().Message,
                    "Không thể cập nhật trạng thái",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                Tai();
                _dangXuLyTrangThai = false;
            }
        }));
    }

        private void LuuTrangThaiDatBan(
        DatBan phieu,
        string trangThaiMoi)
    {
        if (trangThaiMoi != "Chờ xác nhận"
            && trangThaiMoi != "Đã hủy")
        {
            throw new InvalidOperationException(
                "Trạng thái không hợp lệ.");
        }

        using var db = Program.NewDb();

        using var transaction =
            db.Database.BeginTransaction(IsolationLevel.Serializable);

        var row = db.DatBans.Single(x => x.Id == phieu.Id);

        if (row.BatDauPhucVuLuc != null
            || row.TrangThai == "Đã đến")
        {
            throw new InvalidOperationException(
                "Phiếu đã tiếp nhận phục vụ. Hãy xử lý tại POS.");
        }

        if (row.TrangThai != phieu.TrangThai)
        {
            throw new InvalidOperationException(
                "Phiếu đã thay đổi ở nơi khác. Hãy tải lại.");
        }

        row.TrangThai = trangThaiMoi;

        if (!string.IsNullOrWhiteSpace(row.MaDatBan))
        {
            var trangThaiWeb = trangThaiMoi == "Đã hủy"
                ? "Đã hủy"
                : "Đang chờ duyệt";

            db.Database.ExecuteSqlInterpolated($@"
                UPDATE dbo.Web_DatBan
                SET TrangThai = {trangThaiWeb},
                    NgayCapNhat = SYSDATETIME()
                WHERE MaDatBan = {row.MaDatBan};
            ");

            var donHangs = db.DonHangs
                .Where(x => x.MaDatBan == row.MaDatBan)
                .ToList();

            foreach (var don in donHangs)
            {
                if (don.TrangThai == TrangThaiDon.DaThanhToan)
                {
                    throw new InvalidOperationException(
                        "Đơn đã thanh toán. Không thể đổi trạng thái.");
                }

                don.TrangThai = trangThaiMoi == "Đã hủy"
                    ? TrangThaiDon.DaHuy
                    : TrangThaiDon.Moi;
            }
        }

        db.SaveChanges();
        transaction.Commit();
    }

    private void Grid_CellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
    {
        var cell = _grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
        if (_grid.Rows[e.RowIndex].DataBoundItem is not DatBan datBan)
            return;

        var trangThaiDaLuu = _db.Entry(datBan).Property(d => d.TrangThai).OriginalValue;
        var daDen = datBan.TrangThai == "Đã đến" || trangThaiDaLuu == "Đã đến";

        if (cell.OwningColumn.Name == nameof(DatBan.BanId))
        {
            e.Cancel = daDen;
            return;
        }

        if (cell.OwningColumn.Name == nameof(DatBan.TrangThai) && cell is DataGridViewComboBoxCell combo)
        {
            if (daDen)
            {
                e.Cancel = true;
                return;
            }

            combo.DataSource = new[] { "Chờ xác nhận", "Đã xác nhận", "Đã hủy" };
        }
    }

    private void Grid_CellValidating(object? sender, DataGridViewCellValidatingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || !_grid.IsCurrentCellInEditMode) return;
        var colName = _grid.Columns[e.ColumnIndex].Name;

        if (colName == "Gia")
        {
            var raw = (e.FormattedValue?.ToString() ?? "").Trim();
            if (!decimal.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out var gia)
                && !decimal.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out gia))
            {
                MessageBox.Show("Giá món ăn phải là số hợp lệ!", "Dữ liệu không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
                return;
            }
            if (gia < 0)
            {
                MessageBox.Show("Giá phải từ 0 trở lên (không chấp nhận giá nhỏ hơn 0)!", "Dữ liệu chưa hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
                return;
            }
        }
    }

    private ThongTinCocWeb? DocThongTinCocWeb(
        string maDatBan,
        DbTransaction? transaction = null,
        bool khoaBanGhi = false)
    {
        var connection = _db.Database.GetDbConnection();
        bool canClose = connection.State != ConnectionState.Open;

        if (canClose)
            connection.Open();

        try
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = khoaBanGhi
                ? @"SELECT TOP (1) TienCoc, MaGiaoDich, DaThanhToanCoc, TrangThai
                    FROM dbo.Web_DatBan WITH (UPDLOCK, HOLDLOCK)
                    WHERE MaDatBan = @MaDatBan;"
                : @"SELECT TOP (1) TienCoc, MaGiaoDich, DaThanhToanCoc, TrangThai
                    FROM dbo.Web_DatBan
                    WHERE MaDatBan = @MaDatBan;";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@MaDatBan";
            parameter.Value = maDatBan;
            command.Parameters.Add(parameter);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
                return null;

            return new ThongTinCocWeb
            {
                TienCoc = reader.IsDBNull(0) ? 0m : Convert.ToDecimal(reader.GetValue(0)),
                MaGiaoDich = reader.IsDBNull(1) ? "" : reader.GetString(1),
                DaThanhToanCoc = !reader.IsDBNull(2) && Convert.ToBoolean(reader.GetValue(2)),
                TrangThai = reader.IsDBNull(3) ? "" : reader.GetString(3)
            };
        }
        finally
        {
            if (canClose)
                connection.Close();
        }
    }

    private bool KiemTraCocDaXacMinh(DatBan datBan)
    {
        var maDatBan = datBan.MaDatBan?.Trim();
        if (string.IsNullOrWhiteSpace(maDatBan))
            return true; // Phiếu nội bộ chưa liên kết với phiếu Web.

        bool laMaDatBanWeb = maDatBan.StartsWith("HV-", StringComparison.OrdinalIgnoreCase);

        try
        {
            var thongTin = DocThongTinCocWeb(maDatBan);
            if (thongTin == null)
            {
                if (!laMaDatBanWeb)
                    return true; // Phiếu WinForms nội bộ không có bản ghi Web.

                MessageBox.Show(
                    $"Không tìm thấy phiếu {maDatBan} trong Web_DatBan.\nKhông thể xác nhận đặt bàn khi chưa kiểm tra được khoản cọc.",
                    "Không tìm thấy phiếu Web",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            if (thongTin.DaThanhToanCoc)
                return true;

            MessageBox.Show(
                $"Phiếu {maDatBan} chưa được xác minh tiền cọc.\n\n" +
                "Hãy đối chiếu số tiền và mã giao dịch trên tài khoản ngân hàng, " +
                "sau đó sử dụng nút 'Xác nhận cọc' trước khi chuyển trạng thái sang 'Đã xác nhận'.",
                "Cọc chưa được xác minh",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }
        catch (Exception ex)
        {
            if (!laMaDatBanWeb)
                return true;

            MessageBox.Show(
                "Không thể kiểm tra trạng thái cọc trên Website. Hệ thống sẽ không cho xác nhận phiếu này.\n\n" +
                ex.GetBaseException().Message,
                "Không kiểm tra được tiền cọc",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return false;
        }
    }

    private static string CapNhatGhiChuCocDaXacMinh(string? ghiChu, decimal tienCoc)
    {
        const int doDaiToiDa = 300;
        var noiDung = ghiChu ?? "";

        // Xóa nhãn cọc cũ hoặc cọc đang chờ xác minh, rồi ghi đúng một nhãn POS nhận diện.
        noiDung = Regex.Replace(
            noiDung,
            @"Cọc(?:\s+chờ\s+xác\s+minh)?\s*:\s*[\d\.,]+\s*đ?",
            "",
            RegexOptions.IgnoreCase);

        noiDung = Regex.Replace(noiDung, @"\s*\|\s*\|+\s*", " | ");
        noiDung = noiDung.Trim().Trim('|').Trim();

        var nhanCoc = $"Cọc: {tienCoc:N0}đ";
        if (string.IsNullOrWhiteSpace(noiDung))
            return nhanCoc.Length <= doDaiToiDa ? nhanCoc : nhanCoc[..doDaiToiDa];

        if (nhanCoc.Length >= doDaiToiDa)
            return nhanCoc[..doDaiToiDa];

        int doDaiNoiDungToiDa = doDaiToiDa - nhanCoc.Length - 3;

        if (noiDung.Length > doDaiNoiDungToiDa)
            noiDung = noiDung[..doDaiNoiDungToiDa].TrimEnd();

        return $"{noiDung} | {nhanCoc}";
    }

        private void XacNhanCoc()
    {
        if (_dangTai || _dangXuLyTrangThai)
        {
            return;
        }

        if (_grid.CurrentRow?.DataBoundItem is not DatBan phieu)
        {
            MessageBox.Show("Hãy chọn phiếu đặt bàn.");
            return;
        }

        _dangXuLyTrangThai = true;

        try
        {
            _grid.EndEdit();
            (BindingContext?[_list] as CurrencyManager)
            ?.EndCurrentEdit();
            // Dùng trạng thái đã lưu làm cơ sở kiểm tra.
            phieu.TrangThai = _db.Entry(phieu)
                .Property(x => x.TrangThai).OriginalValue;

            DuyetVaXacNhanCoc(phieu);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.GetBaseException().Message,
                "Không thể xác nhận cọc",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            Tai();
            _dangXuLyTrangThai = false;
        }
    }

            private static Button Btn(
            string text,
            Color c,
            EventHandler h)
        {
            var b = new Button
            {
                Text = text,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10, 6, 10, 6),
                Margin = new Padding(0, 0, 8, 6),
                BackColor = c,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };

            b.FlatAppearance.BorderSize = 0;
            b.Click += h;

            return b;
        }
    private void BuildColumns()
    {
        foreach (var p in typeof(T).GetProperties())
        {
            var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
            bool simple = t.IsPrimitive || t == typeof(string) || t == typeof(decimal) || t == typeof(DateTime);
            if (!simple || !p.CanWrite) continue;
            DataGridViewColumn col = t == typeof(bool) ? new DataGridViewCheckBoxColumn() : new DataGridViewTextBoxColumn();
            col.DataPropertyName = p.Name;
            col.Name = p.Name;
            col.HeaderText = p.Name;
            col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            if (p.Name == "Id") col.ReadOnly = true;
            if (t == typeof(decimal)) col.DefaultCellStyle.Format = "N0";
            _grid.Columns.Add(col);
        }
        _configure?.Invoke(_grid, _db);
        CanChinhGiaoDienDatBan();
        _grid.DataSource = _list;
        
    }

    public void Tai()
        {
            _dangTai = true;

            try
            {
                _grid.CancelEdit();

                _db.ChangeTracker.Clear();

                _configure?.Invoke(_grid, _db);
                CanChinhGiaoDienDatBan();
                var q = _query != null
                    ? _query(_db)
                    : _db.Set<T>();

                _list.RaiseListChangedEvents = false;

                try
                {
                    _list.Clear();

                    foreach (var x in q.AsNoTracking().ToList())
                    {
                        if (x is DatBan phieu && phieu.TrangThai == "Đã huỷ")
                        {
                            phieu.TrangThai = "Đã hủy";
                        }

                        _db.Attach(x);
                        _list.Add(x);
                    }
                }
                finally
                {
                    _list.RaiseListChangedEvents = true;
                    _list.ResetBindings();
                }

                _txtSearch.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.GetBaseException().Message,
                    "Lỗi tải dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _dangTai = false;
            }
        }

    private string TaoMaUuDaiMoi()
    {
        string ma;

        do
        {
            // Giữ mã dài 7 ký tự, tương đương mã mặc định cũ UUDAI10.
            ma = "U" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        }
        while (_db.WebUuDais.AsNoTracking().Any(u => u.Ma == ma)
               || _list.OfType<WebUuDai>().Any(u =>
                   string.Equals(u.Ma, ma, StringComparison.OrdinalIgnoreCase)));

        return ma;
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
        else if (item is WebUuDai voucher)
        {
            voucher.Ma = TaoMaUuDaiMoi();
            voucher.MoTa = "Giảm 10% tiền món";
            voucher.PhanTramGiam = 10;
            voucher.GiamToiDa = 200000;
            voucher.HanDung = DateTime.Today.AddMonths(1);
            voucher.HoatDong = true;
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
            MessageBox.Show($"Đã gán ảnh '{mon.HinhAnh}'. Hãy bấm 'Lưu thay đổi' để cập nhật lên Web.", "Chọn ảnh thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

    private void KiemTraBanTruocKhiLuu()
    {
        foreach (var entry in _db.ChangeTracker.Entries<DatBan>())
        {
            if (entry.State != EntityState.Modified)
                continue;

            var trangThai = entry.Property(d => d.TrangThai);
            var banId = entry.Property(d => d.BanId);

            var daDen = trangThai.CurrentValue == "Đã đến" || trangThai.OriginalValue == "Đã đến";
            if (!daDen || banId.CurrentValue == banId.OriginalValue)
                continue;

            banId.CurrentValue = banId.OriginalValue;
            banId.IsModified = false;

            MessageBox.Show(
                $"Phiếu #{entry.Entity.Id} đã đến. Hệ thống giữ bàn đã lưu.",
                "Không thể đổi bàn",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void BatDauPhucVu()
    {
        if (_grid.CurrentRow?.DataBoundItem is not DatBan phieu || phieu.BanId is not int banId)
        {
            MessageBox.Show("Hãy chọn phiếu đã được xếp bàn.");
            return;
        }

        if (!KiemTraCocDaXacMinh(phieu))
            return;

        using var db = Program.NewDb();
        using var tx = db.Database.BeginTransaction();

        var bayGio = DateTime.Now;
        var denLuc = bayGio.AddMinutes(30);

        var soPhieu = db.DatBans
            .Where(d => d.Id == phieu.Id
                        && d.BanId == banId
                        && d.TrangThai == "Đã xác nhận"
                        && d.BatDauPhucVuLuc == null
                        && d.NgayGio <= denLuc)
            .ExecuteUpdate(s => s.SetProperty(d => d.BatDauPhucVuLuc, (DateTime?)bayGio));

        if (soPhieu == 0)
        {
            tx.Rollback();
            MessageBox.Show("Phiếu chưa tới giờ tiếp nhận hoặc trạng thái đã thay đổi.");
            return;
        }

        var soBan = db.Bans
            .Where(b => b.Id == banId && (b.TrangThai == "Trống" || b.TrangThai == "Đã đặt"))
            .ExecuteUpdate(s => s.SetProperty(b => b.TrangThai, "Đang dùng"));

        if (soBan == 0)
        {
            tx.Rollback();
            MessageBox.Show("Bàn đang có khách hoặc đang bảo trì.");
            return;
        }

        tx.Commit();

        _db.ChangeTracker.Clear();
        Tai();
    }

    private void Luu()
    {
        if (!_grid.EndEdit()) return;
        foreach (var x in _list)
        {
            if (x is DatBan datBan && datBan.TrangThai == "Đã xác nhận")
            {
                var entry = _db.Entry(datBan);
                var statusProperty = entry.Property(d => d.TrangThai);
                if (entry.State == EntityState.Modified && statusProperty.IsModified
                    && !KiemTraCocDaXacMinh(datBan))
                {
                    return;
                }
            }

            if (x is MonAn mon && mon.Gia < 0)
            {
                MessageBox.Show("- Giá phải từ 0 trở lên (không chấp nhận giá nhỏ hơn 0)", "Dữ liệu chưa hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (x is WebUuDai ud)
            {
                ud.Ma = (ud.Ma ?? "").Trim().ToUpperInvariant();
                ud.MoTa = (ud.MoTa ?? "").Trim();
            }
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(x, new ValidationContext(x), results, true))
            {
                MessageBox.Show(string.Join("\n", results.Select(r => "- " + r.ErrorMessage)), "Dữ liệu chưa hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }
        try
        {
            KiemTraBanTruocKhiLuu();
            _db.ChangeTracker.DetectChanges();

            // Lấy những phiếu mới thêm hoặc vừa chỉnh sửa,
            // trước khi gọi SaveChanges().
            var phieuCanDongBo = _db.ChangeTracker
                .Entries<DatBan>()
                .Where(e =>
                    e.State == EntityState.Added
                    || e.State == EntityState.Modified)
                .Select(e => e.Entity)
                .ToList();

            _db.SaveChanges();

            if (typeof(T) == typeof(MonAn) || typeof(T) == typeof(DanhMuc))
                {
                    DongBoMonAnSangWeb();
                }
                else if (typeof(T) == typeof(Ban))
                {
                    DongBoBanSangWeb();
                }
                else if (typeof(T) == typeof(DatBan))
                {
                    DongBoDatBanSangWeb(phieuCanDongBo);
                }

            var thongBao = typeof(T) == typeof(WebUuDai)
                ? "Đã lưu thay đổi mã ưu đãi thành công."
                : "Đã lưu thay đổi và đồng bộ lên Web thành công.";

            MessageBox.Show(thongBao, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Tai();
        }
        catch (DbUpdateException ex)
        {
            MessageBox.Show("Không lưu được (trùng dữ liệu hoặc vi phạm ràng buộc):\n" + ex.GetBaseException().Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.GetBaseException().Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
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
                            VALUES ({0}, {2}, {1}, {3}, {4}, N'', {6}, {5});
                    END",
                    m.TenMon, m.Gia, moTa, tenDm, hinhAnh, m.ConPhucVu, m.Id + 10);
            }
        }
        catch { }
    }

    private void DongBoBanSangWeb()
    {
        try
        {
            var bans = _db.Bans.AsNoTracking().OrderBy(b => b.Id).ToList();
            _db.Database.ExecuteSqlRaw("IF OBJECT_ID('Web_KhuVuc', 'U') IS NOT NULL UPDATE Web_KhuVuc SET HoatDong = 0");
            foreach (var ban in bans)
            {
                var ten = (ban.TenBan ?? "").Trim();
                if (string.IsNullOrEmpty(ten)) continue;
                bool isVip = ten.Contains("VIP", StringComparison.OrdinalIgnoreCase);
                bool choTiec = ban.SoGhe >= 8 || isVip || ten.Contains("Tiệc", StringComparison.OrdinalIgnoreCase) || ten.Contains("Sảnh", StringComparison.OrdinalIgnoreCase);
                decimal phuThu = isVip ? 200_000m : 0m;
                string moTa = isVip
                    ? $"Phòng / Bàn VIP ({ban.SoGhe} ghế) · Trạng thái: {ban.TrangThai}"
                    : $"Bàn tiêu chuẩn ({ban.SoGhe} ghế) · Trạng thái: {ban.TrangThai}";
                bool hoatDong = !string.Equals(ban.TrangThai, "Bảo trì", StringComparison.OrdinalIgnoreCase);

                _db.Database.ExecuteSqlRaw(@"
                    IF OBJECT_ID('Web_KhuVuc', 'U') IS NOT NULL
                    BEGIN
                        IF EXISTS (SELECT 1 FROM Web_KhuVuc WHERE Ten = {0})
                            UPDATE Web_KhuVuc SET MoTa = {1}, PhuThu = {2}, SucChua = {3}, HoatDong = {4}, ChoTiec = {5} WHERE Ten = {0};
                        ELSE
                            INSERT INTO Web_KhuVuc (Ten, MoTa, PhuThu, SucChua, HoatDong, ChoTiec)
                            VALUES ({0}, {1}, {2}, {3}, {4}, {5});
                    END",
                    ten, moTa, phuThu, ban.SoGhe, hoatDong, choTiec);
            }
        }
        catch { }
    }

    private void DongBoDatBanSangWeb( IEnumerable<DatBan> danhSachCanDongBo)
    {
        foreach (var item in danhSachCanDongBo)
        {
            if (item.TrangThai == "Đã đến")
                continue;

            var ma = item.MaDatBan;
            var ttWeb = item.TrangThai switch
            {
                "Đã xác nhận" => "Đã xác nhận",
                "Đã huỷ" or "Đã hủy" => "Đã hủy",
                _ => "Đang chờ duyệt"
            };
            var ttDon = item.TrangThai switch
            {
                "Đã xác nhận" => TrangThaiDon.DaXacNhan,
                "Đã huỷ" or "Đã hủy" => TrangThaiDon.DaHuy,
                _ => TrangThaiDon.Moi
            };

            if (!string.IsNullOrEmpty(ma))
            {
                var don = _db.DonHangs.FirstOrDefault(d => d.MaDatBan == ma);
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
                catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"Không đồng bộ được phiếu {ma} lên Web.",
                            ex);
                    }
            }
            else
            {
                ma = $"WF-{100000 + item.Id}";
                item.MaDatBan = ma;
                item.GhiChu = string.IsNullOrWhiteSpace(item.GhiChu)
                    ? $"[{ma}] Tạo từ WinForms"
                    : $"[{ma}] {item.GhiChu}";

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
        private void XoaDatBan(DatBan phieu)
        {
            if (_dangTai || _dangXuLyTrangThai)
            {
                return;
            }

            if (!_duocXacNhanCoc)
            {
                MessageBox.Show(
                    "Chỉ Admin được xóa phiếu đặt bàn.",
                    "Không có quyền",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            var ketQua = MessageBox.Show(
                $"Bạn có chắc muốn xóa phiếu này?\n\n" +
                $"ID: {phieu.Id}\n" +
                $"Mã đặt bàn: {phieu.MaDatBan}\n" +
                $"Khách hàng: {phieu.TenKhach}\n\n" +
                "Nếu còn dòng khác cùng mã: chỉ xóa dòng được chọn.\n" +
                "Nếu đây là dòng cuối cùng: phiếu Web chuyển sang Đã hủy.\n" +
                "Lịch sử tiền cọc và hóa đơn vẫn được giữ lại.",
                "Xác nhận xóa đặt bàn",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (ketQua != DialogResult.Yes)
            {
                return;
            }

            _dangXuLyTrangThai = true;

            try
            {
                // Dùng context riêng để không lưu nhầm các ô khác đang sửa.
                using var db = Program.NewDb();

                using var transaction = db.Database.BeginTransaction(
                    IsolationLevel.Serializable);

                // Xóa theo ID, không xóa hàng loạt theo mã đặt bàn.
                var row = db.DatBans.SingleOrDefault(
                    x => x.Id == phieu.Id);

                if (row == null)
                {
                    throw new InvalidOperationException(
                        "Phiếu không còn tồn tại. Hãy tải lại danh sách.");
                }

                if (row.BatDauPhucVuLuc != null
                    || row.TrangThai == "Đã đến")
                {
                    throw new InvalidOperationException(
                        "Phiếu đã bắt đầu phục vụ. " +
                        "Không thể xóa tại đây, hãy xử lý tại POS.");
                }

                var ma = row.MaDatBan;

                // Kiểm tra có dòng khác cùng mã hay không.
                var conDongKhacCungMa =
                    !string.IsNullOrWhiteSpace(ma)
                    && db.DatBans.Any(x =>
                        x.MaDatBan == ma
                        && x.Id != row.Id);

                // Chỉ hủy dữ liệu liên kết khi xóa dòng cuối cùng.
                if (!conDongKhacCungMa
                    && !string.IsNullOrWhiteSpace(ma))
                {
                    var donHangs = db.DonHangs
                        .Where(x => x.MaDatBan == ma)
                        .ToList();

                    if (donHangs.Any(x =>
                        x.TrangThai == TrangThaiDon.DaThanhToan))
                    {
                        throw new InvalidOperationException(
                            "Mã đặt bàn này đã có hóa đơn thanh toán. " +
                            "Không thể xóa phiếu cuối cùng vì cần giữ lịch sử.");
                    }

                    db.Database.ExecuteSqlInterpolated($@"
                        UPDATE dbo.Web_DatBan
                        SET TrangThai = N'Đã hủy',
                            LyDoHuy = N'Admin xóa phiếu trên WinForms',
                            NgayCapNhat = SYSDATETIME()
                        WHERE MaDatBan = {ma};
                    ");

                    foreach (var don in donHangs)
                    {
                        don.TrangThai = TrangThaiDon.DaHuy;
                    }
                }

                db.DatBans.Remove(row);

                db.SaveChanges();
                transaction.Commit();

                MessageBox.Show(
                    conDongKhacCungMa
                        ? "Đã xóa dòng được chọn. Phiếu Web được giữ nguyên."
                        : "Đã xóa phiếu Admin. Nếu có phiếu Web liên kết, " +
                        "phiếu đó đã được chuyển sang Đã hủy.",
                    "Thành công",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Không thể xóa phiếu đặt bàn.\n\n" +
                    ex.GetBaseException().Message,
                    "Lỗi xóa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                Tai();
                _dangXuLyTrangThai = false;
            }
        }
    private void Xoa()
        {
            if (_dangTai || _dangXuLyTrangThai)
            {
                return;
            }

            if (_grid.CurrentRow?.DataBoundItem is not T item)
            {
                MessageBox.Show(
                    "Hãy chọn dòng cần xóa.",
                    "Chưa chọn dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            if (_db.Entry(item).State == EntityState.Added)
            {
                _db.Entry(item).State = EntityState.Detached;
                _list.Remove(item);
                return;
            }

            // Phiếu đặt bàn dùng luồng xóa riêng.
            if (item is DatBan phieu)
            {
                XoaDatBan(phieu);
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
            if (typeof(T) == typeof(Ban))
            {
                DongBoBanSangWeb();
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
        _grid.CurrentCell = null;
        foreach (DataGridViewRow r in _grid.Rows)
        {
            bool match = kw.Length == 0 || r.Cells.Cast<DataGridViewCell>().Any(c => (c.Value?.ToString() ?? "").ToLowerInvariant().Contains(kw));
            r.Visible = match;
        }
        cm?.ResumeBinding();
    }
    private bool DuyetVaXacNhanCoc(DatBan phieu)
{
    if (!_duocXacNhanCoc)
    {
        throw new InvalidOperationException(
            "Chỉ Admin được xác nhận cọc và duyệt đặt bàn.");
    }

    if (phieu.Id <= 0)
    {
        throw new InvalidOperationException(
            "Hãy lưu phiếu đặt bàn trước khi duyệt.");
    }

    var ma = phieu.MaDatBan?.Trim();

    if (string.IsNullOrWhiteSpace(ma))
    {
        throw new InvalidOperationException(
            "Phiếu chưa có mã đặt bàn.");
    }

    var thongTin = DocThongTinCocWeb(ma);

    if (thongTin == null)
    {
        throw new InvalidOperationException(
            "Không tìm thấy phiếu tương ứng trong Web_DatBan.");
    }

    if (thongTin.TrangThai != "Đang chờ duyệt"
        && thongTin.TrangThai != "Đã xác nhận")
    {
        throw new InvalidOperationException(
            $"Không thể duyệt phiếu đang ở trạng thái " +
            $"'{thongTin.TrangThai}'.");
    }

    if (thongTin.TienCoc > 0
        && string.IsNullOrWhiteSpace(thongTin.MaGiaoDich))
    {
        throw new InvalidOperationException(
            "Phiếu có tiền cọc nhưng chưa có mã giao dịch.");
    }

    var ketQua = MessageBox.Show(
        "Chỉ chọn Yes sau khi đã đối chiếu giao dịch thực tế " +
        "trong tài khoản ngân hàng của nhà hàng.\n\n" +
        $"Mã đặt bàn: {ma}\n" +
        $"Khách hàng: {phieu.TenKhach}\n" +
        $"Số tiền cọc: {thongTin.TienCoc:N0} đ\n" +
        $"Mã giao dịch: {thongTin.MaGiaoDich}\n" +
        $"Tình trạng cọc: " +
        $"{(thongTin.DaThanhToanCoc ? "Đã xác minh" : "Chưa xác minh")}\n\n" +
        "Xác nhận đã nhận đủ khoản cọc và duyệt đặt bàn?",
        "Đối soát tiền cọc",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Warning,
        MessageBoxDefaultButton.Button2);

    if (ketQua != DialogResult.Yes)
    {
        return false;
    }

    // Context riêng để không lưu nhầm các ô khác đang chỉnh trên lưới.
    using var db = Program.NewDb();

    using var transaction =
        db.Database.BeginTransaction(IsolationLevel.Serializable);

    var danhSach = db.DatBans
        .Where(x => x.MaDatBan == ma)
        .ToList();

    if (danhSach.Count != 1)
    {
        throw new InvalidOperationException(
            "Mã đặt bàn đang bị trùng hoặc không còn tồn tại. " +
            "Hãy xử lý dữ liệu trùng trước khi duyệt.");
    }

    var phieuDb = danhSach[0];

    if (phieuDb.BatDauPhucVuLuc != null
        || phieuDb.TrangThai == "Đã đến")
    {
        throw new InvalidOperationException(
            "Phiếu đã bắt đầu phục vụ. Hãy xử lý tại POS.");
    }

    if (phieuDb.TrangThai != phieu.TrangThai)
    {
        throw new InvalidOperationException(
            "Trạng thái đã thay đổi ở nơi khác. Hãy tải lại.");
    }

    // Chỉ cập nhật nếu thông tin vẫn đúng như lúc mở hộp thoại.
    var soDong = db.Database.ExecuteSqlInterpolated($@"
        UPDATE dbo.Web_DatBan
        SET DaThanhToanCoc = 1,
            NgayThanhToan =
                COALESCE(NgayThanhToan, SYSDATETIME()),
            TrangThai = N'Đã xác nhận',
            NgayCapNhat = SYSDATETIME()
        WHERE MaDatBan = {ma}
          AND TrangThai = {thongTin.TrangThai}
          AND TienCoc = {thongTin.TienCoc}
          AND MaGiaoDich = {thongTin.MaGiaoDich}
          AND DaThanhToanCoc = {thongTin.DaThanhToanCoc};
    ");

    if (soDong != 1)
    {
        throw new InvalidOperationException(
            "Thông tin Web đã thay đổi. " +
            "Hãy tải lại và đối soát lại khoản cọc.");
    }

    phieuDb.TrangThai = "Đã xác nhận";

    phieuDb.GhiChu = CapNhatGhiChuCocDaXacMinh(
        phieuDb.GhiChu,
        thongTin.TienCoc);

    var donHangs = db.DonHangs
        .Where(x => x.MaDatBan == ma)
        .ToList();

    if (donHangs.Any(
        x => x.TrangThai == TrangThaiDon.DaThanhToan))
    {
        throw new InvalidOperationException(
            "Đơn hàng đã thanh toán, không thể duyệt lại.");
    }

    foreach (var don in donHangs)
    {
        don.TrangThai = TrangThaiDon.DaXacNhan;

        don.GhiChu = CapNhatGhiChuCocDaXacMinh(
            don.GhiChu,
            thongTin.TienCoc);
    }

    if (phieuDb.BanId is int banId)
    {
        var ban = db.Bans.Find(banId);

        if (ban != null && ban.TrangThai == "Trống")
        {
            ban.TrangThai = "Đã đặt";
        }
    }

    db.SaveChanges();
    transaction.Commit();

    return true;
}


}
