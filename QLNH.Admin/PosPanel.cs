using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using QLNH.Data.Entities;
using QLNH.Data.Services;

namespace QLNH.Admin;

public partial class PosPanel : UserControl
{
    private TableLayoutPanel mainLayout = null!;
    private FlowLayoutPanel pnlTables = null!;
    private FlowLayoutPanel pnlMenu = null!;
    private Panel pnlBill = null!;
    private Label lblTableStatus = null!;
    private DataGridView dgvOrderDetails = null!;
    private Label lblSubTotal = null!;
    private Label lblTotal = null!;
    private Button btnSaveOrder = null!;
    private Button btnChangeTable = null!;
    private Button btnRemoveItem = null!;
    private Button btnPay = null!;

    // Biến lưu vết bàn và đơn hàng đang mở của bàn đang được chọn
    private Button? currentSelectedTable = null;
    private int? _selectedBanId = null;
    private int? _currentOpenOrderId = null;
    private decimal _currentDeposit = 0;
    private decimal _currentExtraFee = 0;

    public PosPanel()
    {
        InitializeUI();
        LoadDataFromDb();
    }

    private void InitializeUI()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.WhiteSmoke;

        mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 43F));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));

        pnlTables = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White, Padding = new Padding(10) };
        var wrapTables = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
        wrapTables.Controls.Add(pnlTables);

        pnlMenu = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White, Padding = new Padding(10) };
        var wrapMenu = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
        wrapMenu.Controls.Add(pnlMenu);

        pnlBill = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10) };

        lblTableStatus = new Label
        {
            Text = "📋 Chưa chọn bàn",
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = Color.DarkSlateGray,
            Dock = DockStyle.Top,
            Height = 32,
            TextAlign = ContentAlignment.MiddleLeft
        };

        dgvOrderDetails = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            AllowUserToAddRows = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            MultiSelect = false
        };
        dgvOrderDetails.Columns.Add("TenMon", "Tên Món");
        dgvOrderDetails.Columns.Add("SoLuong", "SL");
        dgvOrderDetails.Columns.Add("Gia", "Giá");
        dgvOrderDetails.Columns.Add("ThanhTien", "Tổng");
        dgvOrderDetails.Columns["SoLuong"].Width = 40;

        var pnlPaymentArea = new Panel { Dock = DockStyle.Bottom, Height = 172, Padding = new Padding(0, 6, 0, 0) };
        lblSubTotal = new Label { Text = "Đã cọc: 0 đ", Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), ForeColor = Color.SeaGreen, Dock = DockStyle.Top, Height = 24, TextAlign = ContentAlignment.MiddleRight };
        lblTotal = new Label { Text = "Còn phải thu: 0 đ", Font = new Font("Segoe UI", 14.5F, FontStyle.Bold), ForeColor = Color.Firebrick, Dock = DockStyle.Top, Height = 34, TextAlign = ContentAlignment.MiddleRight };

        var pnlActionRow = new TableLayoutPanel { Dock = DockStyle.Top, Height = 44, ColumnCount = 3, RowCount = 1, Margin = new Padding(0, 4, 0, 4) };
        pnlActionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
        pnlActionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31F));
        pnlActionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31F));

        btnSaveOrder = new Button
        {
            Text = "💾 LƯU TẠM",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            BackColor = Color.SteelBlue,
            ForeColor = Color.White,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 2, 3, 2)
        };
        btnSaveOrder.FlatAppearance.BorderSize = 0;
        btnSaveOrder.Click += BtnSaveOrder_Click;

        btnChangeTable = new Button
        {
            Text = "🔄 ĐỔI BÀN",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            BackColor = Color.DarkOrange,
            ForeColor = Color.White,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(3, 2, 3, 2)
        };
        btnChangeTable.FlatAppearance.BorderSize = 0;
        btnChangeTable.Click += BtnChangeTable_Click;

        btnRemoveItem = new Button
        {
            Text = "➖ XÓA MÓN",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            BackColor = Color.IndianRed,
            ForeColor = Color.White,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(3, 2, 0, 2)
        };
        btnRemoveItem.FlatAppearance.BorderSize = 0;
        btnRemoveItem.Click += BtnRemoveItem_Click;

        pnlActionRow.Controls.Add(btnSaveOrder, 0, 0);
        pnlActionRow.Controls.Add(btnChangeTable, 1, 0);
        pnlActionRow.Controls.Add(btnRemoveItem, 2, 0);

        btnPay = new Button
        {
            Text = "💳 THANH TOÁN & IN HÓA ĐƠN",
            Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
            BackColor = Color.MediumSeaGreen,
            ForeColor = Color.White,
            Dock = DockStyle.Bottom,
            Height = 48,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnPay.FlatAppearance.BorderSize = 0;
        btnPay.Click += BtnPay_Click;

        pnlPaymentArea.Controls.Add(pnlActionRow);
        pnlPaymentArea.Controls.Add(lblTotal);
        pnlPaymentArea.Controls.Add(lblSubTotal);
        pnlPaymentArea.Controls.Add(btnPay);
        pnlBill.Controls.Add(dgvOrderDetails);
        pnlBill.Controls.Add(lblTableStatus);
        pnlBill.Controls.Add(pnlPaymentArea);

        var wrapBill = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
        wrapBill.Controls.Add(pnlBill);

        mainLayout.Controls.Add(wrapTables, 0, 0);
        mainLayout.Controls.Add(wrapMenu, 1, 0);
        mainLayout.Controls.Add(wrapBill, 2, 0);
        this.Controls.Add(mainLayout);
    }

    public void LoadDataFromDb()
    {
        pnlTables.Controls.Clear();
        pnlMenu.Controls.Clear();
        currentSelectedTable = null;

        try
        {
            using var db = Program.NewDb();

            var dsBan = db.Set<Ban>().OrderBy(b => b.TenBan).ToList();
            var bayGio = DateTime.Now;
            var denLuc = bayGio.AddMinutes(30);

            var gioHenTheoBan = db.DatBans.AsNoTracking()
                .Where(d => d.BanId.HasValue
                            && d.TrangThai == "Đã xác nhận"
                            && d.BatDauPhucVuLuc == null
                            && d.NgayGio <= denLuc)
                .GroupBy(d => d.BanId!.Value)
                .Select(g => new { BanId = g.Key, GioHen = g.Min(d => d.NgayGio) })
                .ToDictionary(x => x.BanId, x => x.GioHen);

            foreach (var ban in dsBan)
            {
                var tinhTrang = ban.TrangThai == "Đã đặt" ? "Trống" : ban.TrangThai;

                if (gioHenTheoBan.TryGetValue(ban.Id, out var gioHen))
                {
                    if (tinhTrang == "Trống")
                        tinhTrang = $"Đã đặt · {gioHen:HH:mm}";
                    else if (tinhTrang == "Đang dùng")
                        tinhTrang = bayGio >= gioHen.AddMinutes(15)
                            ? $"Đang dùng\nCần xếp bàn · {gioHen:HH:mm}"
                            : $"Đang dùng\nCó hẹn · {gioHen:HH:mm}";
                }

                var btnTable = new Button
                {
                    Text = $"{ban.TenBan}\n{tinhTrang}",
                    Size = new Size(85, 80),
                    Margin = new Padding(5),
                    BackColor = tinhTrang == "Trống" ? Color.Gainsboro : Color.Orange,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Tag = ban
                };
                btnTable.FlatAppearance.BorderSize = 0;
                btnTable.Click += Table_Click;
                pnlTables.Controls.Add(btnTable);

                if (_selectedBanId.HasValue && ban.Id == _selectedBanId.Value)
                {
                    btnTable.BackColor = Color.SeaGreen;
                    btnTable.ForeColor = Color.White;
                    currentSelectedTable = btnTable;
                }
            }

            var dsMon = db.Set<MonAn>().Where(m => m.ConPhucVu).OrderBy(m => m.DanhMucId).ThenBy(m => m.Id).ToList();
            foreach (var mon in dsMon)
            {
                var btnFood = new Button
                {
                    Text = $"{mon.TenMon}\n{mon.Gia:N0} đ",
                    Size = new Size(130, 90),
                    Margin = new Padding(5),
                    BackColor = Color.LightSkyBlue,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Tag = mon
                };
                btnFood.FlatAppearance.BorderSize = 0;
                btnFood.Click += Food_Click;
                pnlMenu.Controls.Add(btnFood);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi tải dữ liệu từ DB: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static IQueryable<DonHang> TimDonDangPhucVu(QLNH.Data.AppDbContext db, int banId)
    {
        var phieuDangPhucVu = db.DatBans.Where(d =>
            d.BanId == banId
            && d.TrangThai == "Đã xác nhận"
            && d.BatDauPhucVuLuc != null);

        return db.DonHangs.Where(o =>
            o.BanId == banId
            && o.TrangThai != "Đã thanh toán"
            && o.TrangThai != "Hoàn thành"
            && o.TrangThai != "Đã hủy"
            && o.TrangThai != "Đã huỷ"
            && db.Bans.Any(b => b.Id == banId && b.TrangThai == "Đang dùng")
            && (
                // Đơn đặt trước của đúng phiếu đang phục vụ.
                (o.MaDatBan != null && o.MaDatBan != ""
                    && phieuDangPhucVu.Any(d => d.MaDatBan == o.MaDatBan))
                // Đơn khách vãng lai đang sử dụng bàn.
                || ((o.MaDatBan == null || o.MaDatBan == "")
                    && !phieuDangPhucVu.Any())
            ));
    }

    private static string? KiemTraGiuBan(QLNH.Data.AppDbContext db, int banId, int? donHangId = null)
    {
        if (donHangId is int id)
        {
            return TimDonDangPhucVu(db, banId).Any(o => o.Id == id)
                ? null
                : "Đơn đang mở đã thay đổi. Vui lòng chọn lại bàn.";
        }

        if (db.Bans.Any(b => b.Id == banId && b.TrangThai == "Đang dùng"))
        {
            return "Bàn đang dùng nhưng chưa tìm được đơn đang phục vụ. "
                 + "Hãy kiểm tra mã đặt bàn trên phiếu và đơn hàng.";
        }

        var bayGio = DateTime.Now;
        var denLuc = bayGio.AddMinutes(30);

        var gioHen = db.DatBans.AsNoTracking()
            .Where(d => d.BanId == banId
                        && d.TrangThai == "Đã xác nhận"
                        && d.BatDauPhucVuLuc == null
                        && d.NgayGio <= denLuc)
            .OrderBy(d => d.NgayGio)
            .Select(d => (DateTime?)d.NgayGio)
            .FirstOrDefault();

        if (gioHen is DateTime gio)
        {
            return $"Bàn đang giữ cho khách có hẹn lúc {gio:HH:mm}. "
                 + "Nếu khách đặt bàn đã tới, hãy bấm Bắt đầu phục vụ.";
        }

        return null;
    }

    private void Table_Click(object? sender, EventArgs e)
    {
        if (sender is Button clickedTable && clickedTable.Tag is Ban ban)
        {
            using var db = Program.NewDb();
            var donHangId = TimDonDangPhucVu(db, ban.Id)
                .OrderByDescending(o => o.Id)
                .Select(o => (int?)o.Id)
                .FirstOrDefault();

            if (donHangId == null)
            {
                try
                {
                    donHangId = MoDonDatTruocTaiPos(db, ban.Id, out var daTuChoi);
                    if (daTuChoi)
                        return;
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show(ex.Message, "Không mở được đơn");
                    return;
                }
            }

            var loi = KiemTraGiuBan(db, ban.Id, donHangId);
            if (loi != null)
            {
                MessageBox.Show(loi, "Bàn đang được giữ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (currentSelectedTable != null && currentSelectedTable.Tag is Ban)
            {
                currentSelectedTable.BackColor =
                    currentSelectedTable.Text.EndsWith("\nTrống", StringComparison.Ordinal)
                        ? Color.Gainsboro
                        : Color.Orange;
                currentSelectedTable.ForeColor = Color.Black;
            }

            clickedTable.BackColor = Color.SeaGreen;
            clickedTable.ForeColor = Color.White;
            currentSelectedTable = clickedTable;
            _selectedBanId = ban.Id;

            TaiDonDangMoCuaBan(ban);
        }
    }

    private int? MoDonDatTruocTaiPos(QLNH.Data.AppDbContext db, int banId, out bool daTuChoi)
    {
        daTuChoi = false;
        var bayGio = DateTime.Now;
        var denLuc = bayGio.AddMinutes(30);

        var phieu = db.DatBans.AsNoTracking()
            .Where(d => d.BanId == banId
                        && d.TrangThai == "Đã xác nhận"
                        && d.BatDauPhucVuLuc == null
                        && d.NgayGio <= denLuc)
            .OrderBy(d => d.NgayGio)
            .ThenBy(d => d.Id)
            .FirstOrDefault();

        if (phieu == null)
            return null;

        var banCoTheNhanKhach = db.Bans.Any(b => b.Id == banId
            && (b.TrangThai == "Trống" || b.TrangThai == "Đã đặt"));

        if (!banCoTheNhanKhach)
            return null;

        if (string.IsNullOrWhiteSpace(phieu.MaDatBan))
            throw new InvalidOperationException("Phiếu này chưa có mã đặt bàn.");

        var xacNhan = MessageBox.Show(
            $"Xác nhận khách của phiếu {phieu.MaDatBan} hẹn lúc {phieu.NgayGio:HH:mm} đã tới để mở order?",
            "Bắt đầu phục vụ",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (xacNhan != DialogResult.Yes)
        {
            daTuChoi = true;
            return null;
        }

        using var tx = db.Database.BeginTransaction();

        var nhanLuc = DateTime.Now;
        var daNhan = db.DatBans
            .Where(d => d.Id == phieu.Id
                        && d.BanId == banId
                        && d.MaDatBan == phieu.MaDatBan
                        && d.TrangThai == "Đã xác nhận"
                        && d.BatDauPhucVuLuc == null)
            .ExecuteUpdate(s => s.SetProperty(d => d.BatDauPhucVuLuc, (DateTime?)nhanLuc));

        if (daNhan != 1)
            throw new InvalidOperationException("Phiếu đã thay đổi. Hãy chọn lại bàn.");

        var daMoBan = db.Bans
            .Where(b => b.Id == banId && (b.TrangThai == "Trống" || b.TrangThai == "Đã đặt"))
            .ExecuteUpdate(s => s.SetProperty(b => b.TrangThai, "Đang dùng"));

        if (daMoBan != 1)
            throw new InvalidOperationException("Bàn đã đổi trạng thái. Hãy chọn lại bàn.");

        var cacDon = db.DonHangs
            .Where(o => o.MaDatBan == phieu.MaDatBan)
            .Take(2)
            .ToList();

        if (cacDon.Count > 1)
            throw new InvalidOperationException("Phiếu này đang liên kết với nhiều đơn.");

        var don = cacDon.SingleOrDefault();
        if (don == null)
        {
            don = new DonHang
            {
                BanId = banId,
                MaDatBan = phieu.MaDatBan,
                TenKhach = phieu.TenKhach,
                SoDienThoai = phieu.SoDienThoai,
                TrangThai = TrangThaiDon.DaXacNhan,
                NgayTao = nhanLuc
            };

            db.DonHangs.Add(don);
            db.SaveChanges();
        }

        if (!TimDonDangPhucVu(db, banId).Any(o => o.Id == don.Id))
            throw new InvalidOperationException("Đơn đã đóng hoặc đang gắn với bàn khác.");

        tx.Commit();
        return don.Id;
    }

    private void TaiDonDangMoCuaBan(Ban ban)
    {
        dgvOrderDetails.Rows.Clear();
        _currentOpenOrderId = null;
        _currentDeposit = 0;
        _currentExtraFee = 0;

        try
        {
            using var db = Program.NewDb();
            var openOrder = TimDonDangPhucVu(db, ban.Id)
                .Include(d => d.ChiTiets)
                .ThenInclude(c => c.MonAn)
                .OrderByDescending(d => d.Id)
                .FirstOrDefault();

            if (openOrder != null)
            {
                _currentOpenOrderId = openOrder.Id;
                _currentDeposit = OrdersPanel.TachTienCoc(openOrder.GhiChu);
                lblTableStatus.Text = $"📋 {ban.TenBan} — Đơn #{openOrder.Id} ({openOrder.TrangThai})";
                decimal sumMon = 0;
                foreach (var ct in openOrder.ChiTiets)
                {
                    var tenMon = ct.MonAn?.TenMon ?? "Món";
                    var tt = ct.SoLuong * ct.DonGia;
                    sumMon += tt;
                    dgvOrderDetails.Rows.Add(tenMon, ct.SoLuong, ct.DonGia.ToString("N0"), tt.ToString("N0"));
                }
                // Giữ lại phần phụ thu khu vực / VAT / giảm giá từ đơn đặt bàn Web (nếu có)
                if (openOrder.TongTien > 0 && sumMon > 0)
                {
                    _currentExtraFee = openOrder.TongTien - sumMon;
                }
            }
            else
            {
                lblTableStatus.Text = $"📋 {ban.TenBan} — Bàn mới (chưa gọi món)";
            }
        }
        catch { }

        UpdateTotal();
    }

    private void Food_Click(object? sender, EventArgs e)
    {
        if (currentSelectedTable == null)
        {
            MessageBox.Show("Vui lòng chọn Bàn trước khi gọi món!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (sender is Button clickedFood && clickedFood.Tag is MonAn mon)
        {
            string tenMon = mon.TenMon;
            decimal gia = mon.Gia;

            bool daTonTai = false;
            foreach (DataGridViewRow row in dgvOrderDetails.Rows)
            {
                if (row.Cells["TenMon"].Value?.ToString() == tenMon)
                {
                    int slHienTai = Convert.ToInt32(row.Cells["SoLuong"].Value);
                    row.Cells["SoLuong"].Value = slHienTai + 1;
                    row.Cells["ThanhTien"].Value = ((slHienTai + 1) * gia).ToString("N0");
                    daTonTai = true;
                    break;
                }
            }

            if (!daTonTai)
            {
                dgvOrderDetails.Rows.Add(tenMon, 1, gia.ToString("N0"), gia.ToString("N0"));
            }
            UpdateTotal();
        }
    }

    private void BtnRemoveItem_Click(object? sender, EventArgs e)
    {
        if (dgvOrderDetails.CurrentRow == null)
        {
            MessageBox.Show("Hãy chọn một món trong danh sách hóa đơn để giảm hoặc xóa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var row = dgvOrderDetails.CurrentRow;
        int sl = Convert.ToInt32(row.Cells["SoLuong"].Value);
        decimal gia = Convert.ToDecimal(row.Cells["Gia"].Value.ToString()!.Replace(",", "").Replace(".", ""));
        if (sl > 1)
        {
            row.Cells["SoLuong"].Value = sl - 1;
            row.Cells["ThanhTien"].Value = ((sl - 1) * gia).ToString("N0");
        }
        else
        {
            dgvOrderDetails.Rows.Remove(row);
        }
        UpdateTotal();
    }

    private decimal TinhTongMon()
    {
        decimal total = 0;
        foreach (DataGridViewRow row in dgvOrderDetails.Rows)
        {
            string thanhTienStr = row.Cells["ThanhTien"].Value.ToString()!.Replace(",", "").Replace(".", "");
            total += Convert.ToDecimal(thanhTienStr);
        }
        return total;
    }

    private decimal TinhTongHoaDon() => Math.Max(0, TinhTongMon() + _currentExtraFee);

    private void UpdateTotal()
    {
        decimal total = TinhTongHoaDon();
        decimal conLai = Math.Max(0, total - _currentDeposit);
        lblSubTotal.Text = $"Tổng hóa đơn: {total:N0} đ   |   Đã cọc: {_currentDeposit:N0} đ";
        lblTotal.Text = $"Còn phải thu: {conLai:N0} đ";
    }

    private DonHang LuuDonHangCuaBan(QLNH.Data.AppDbContext db, Ban ban, string trangThaiDon, string trangThaiBan)
    {
        var loi = KiemTraGiuBan(db, ban.Id, _currentOpenOrderId);
        if (loi != null)
            throw new InvalidOperationException(loi);

        decimal totalAmount = TinhTongHoaDon();
        DonHang? donHang = null;
        if (_currentOpenOrderId.HasValue)
        {
            donHang = db.DonHangs.Include(d => d.ChiTiets).FirstOrDefault(d => d.Id == _currentOpenOrderId.Value);
        }

        if (donHang == null)
        {
            donHang = new DonHang
            {
                TenKhach = $"Khách tại bàn ({ban.TenBan})",
                BanId = ban.Id,
                TrangThai = trangThaiDon,
                TongTien = totalAmount,
                NgayTao = DateTime.Now
            };
            db.DonHangs.Add(donHang);
            db.SaveChanges();
            _currentOpenOrderId = donHang.Id;
        }
        else
        {
            donHang.TrangThai = trangThaiDon;
            donHang.TongTien = totalAmount;
            db.ChiTietDonHangs.RemoveRange(donHang.ChiTiets);
            db.SaveChanges();
        }

        var allMons = db.MonAns.ToList();
        foreach (DataGridViewRow row in dgvOrderDetails.Rows)
        {
            string tenMon = row.Cells["TenMon"].Value.ToString()!;
            var monAn = allMons.FirstOrDefault(m => m.TenMon == tenMon);
            if (monAn != null)
            {
                db.ChiTietDonHangs.Add(new ChiTietDonHang
                {
                    DonHangId = donHang.Id,
                    MonAnId = monAn.Id,
                    SoLuong = Convert.ToInt32(row.Cells["SoLuong"].Value),
                    DonGia = Convert.ToDecimal(row.Cells["Gia"].Value.ToString()!.Replace(",", "").Replace(".", ""))
                });
            }
        }

        var banUpdate = db.Bans.Find(ban.Id);
        if (banUpdate != null)
        {
            banUpdate.TrangThai = trangThaiBan;
        }
        db.SaveChanges();
        return donHang;
    }

    private void BtnChangeTable_Click(object? sender, EventArgs e)
    {
        if (currentSelectedTable?.Tag is not Ban banCu)
        {
            MessageBox.Show("Vui lòng chọn bàn hiện tại của khách trước khi đổi bàn!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (_currentOpenOrderId == null && dgvOrderDetails.Rows.Count == 0 && banCu.TrangThai == "Trống")
        {
            MessageBox.Show($"{banCu.TenBan} đang trống và chưa có món nào để chuyển bàn.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var db = Program.NewDb();
        var dsBanTrong = db.Bans.AsNoTracking()
            .Where(b => b.Id != banCu.Id && b.TrangThai == "Trống")
            .OrderBy(b => b.TenBan)
            .ToList()
            .Where(b => KiemTraGiuBan(db, b.Id) == null)
            .ToList();

        if (dsBanTrong.Count == 0)
        {
            MessageBox.Show("Hiện không còn bàn trống nào để chuyển sang!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dlg = new Form
        {
            Text = $"Đổi bàn cho khách từ {banCu.TenBan}",
            Size = new Size(360, 190),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Color.White
        };
        var lbl = new Label { Text = $"Chuyển từ {banCu.TenBan} sang bàn trống:", Left = 20, Top = 20, AutoSize = true, Font = new Font("Segoe UI", 10.5F, FontStyle.Bold) };
        var cbo = new ComboBox { Left = 20, Top = 52, Width = 300, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 11F), DataSource = dsBanTrong, DisplayMember = "TenBan", ValueMember = "Id" };
        var btnOk = new Button { Text = "Xác nhận đổi bàn", Left = 170, Top = 100, Width = 150, Height = 36, BackColor = Color.DarkOrange, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.OK, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
        dlg.Controls.AddRange(new Control[] { lbl, cbo, btnOk });
        dlg.AcceptButton = btnOk;

        if (dlg.ShowDialog(this) != DialogResult.OK || cbo.SelectedItem is not Ban chon) return;

        var donIdTruocKhiDoi = _currentOpenOrderId;

        try
        {
            using var tx = db.Database.BeginTransaction();
            var loi = KiemTraGiuBan(db, chon.Id);
            if (loi != null)
                throw new InvalidOperationException(loi);

            if (_currentOpenOrderId == null && dgvOrderDetails.Rows.Count > 0)
            {
                LuuDonHangCuaBan(db, banCu, TrangThaiDon.DaXacNhan, "Đang dùng");
            }

            if (_currentOpenOrderId is not int donId)
                throw new InvalidOperationException("Bàn chưa có đơn đang phục vụ.");

            var don = TimDonDangPhucVu(db, banCu.Id)
                .SingleOrDefault(o => o.Id == donId)
                ?? throw new InvalidOperationException("Đơn đã thay đổi. Vui lòng chọn lại bàn.");

            var bCu = db.Bans.Find(banCu.Id)
                ?? throw new InvalidOperationException("Không tìm thấy bàn cũ.");

            var bMoi = db.Bans.Find(chon.Id)
                ?? throw new InvalidOperationException("Không tìm thấy bàn mới.");

            if (!string.IsNullOrWhiteSpace(don.MaDatBan))
            {
                var phieu = db.DatBans.SingleOrDefault(d =>
                    d.MaDatBan == don.MaDatBan
                    && d.BanId == banCu.Id
                    && d.TrangThai == "Đã xác nhận"
                    && d.BatDauPhucVuLuc != null)
                    ?? throw new InvalidOperationException("Không tìm thấy phiếu đang phục vụ của đơn này.");

                if (phieu.SoNguoi > bMoi.SoGhe)
                    throw new InvalidOperationException("Bàn mới không đủ chỗ.");

                DongBoDoiBanLenWeb(db, don.MaDatBan, bMoi.TenBan);
                phieu.BanId = bMoi.Id;
            }

            don.BanId = bMoi.Id;
            if (don.TenKhach == $"Khách tại bàn ({bCu.TenBan})")
                don.TenKhach = $"Khách tại bàn ({bMoi.TenBan})";

            bCu.TrangThai = "Trống";
            bMoi.TrangThai = "Đang dùng";

            db.SaveChanges();
            tx.Commit();
        }
        catch (Exception ex)
        {
            _currentOpenOrderId = donIdTruocKhiDoi;
            MessageBox.Show("Lỗi khi đổi bàn: " + ex.GetBaseException().Message);
            return;
        }

        _selectedBanId = chon.Id;
        LoadDataFromDb();
        TaiDonDangMoCuaBan(chon);

        MessageBox.Show($"Đã chuyển đơn từ {banCu.TenBan} sang {chon.TenBan}.");
    }

    private static void DongBoDoiBanLenWeb(QLNH.Data.AppDbContext db, string maDatBan, string tenBanMoi)
    {
        db.Database.ExecuteSqlInterpolated($"""
            DECLARE @Ma nvarchar(30) = {maDatBan};
            DECLARE @TenBan nvarchar(100) = {tenBanMoi.Trim()};
            DECLARE @GioHen datetime2;

            SELECT @GioHen = NgayGio
            FROM dbo.Web_DatBan WITH (UPDLOCK, HOLDLOCK)
            WHERE MaDatBan = @Ma
              AND TrangThai = N'Đã xác nhận';

            IF @GioHen IS NULL
            BEGIN
                IF LEFT(@Ma, 3) = N'WF-'
                   AND NOT EXISTS (
                       SELECT 1 FROM dbo.Web_DatBan WHERE MaDatBan = @Ma
                   )
                    RETURN;

                THROW 50001, N'Không tìm thấy phiếu Web đang xác nhận để đổi bàn.', 1;
            END;

            DECLARE @KhuVucMoi int;

            SELECT @KhuVucMoi = MIN(Id)
            FROM dbo.Web_KhuVuc
            WHERE LTRIM(RTRIM(Ten)) = @TenBan AND HoatDong = 1
            HAVING COUNT(*) = 1;

            IF @KhuVucMoi IS NULL
                THROW 50002, N'Không tìm thấy bàn mới tương ứng trên Web.', 1;

            IF EXISTS (
                SELECT 1
                FROM dbo.Web_DatBan WITH (UPDLOCK, HOLDLOCK)
                WHERE KhuVucId = @KhuVucMoi
                  AND MaDatBan <> @Ma
                  AND TrangThai NOT IN (
                      N'Đã hủy', N'Đã huỷ', N'Đã hoàn thành', N'Đã thanh toán'
                  )
                  AND (TrangThai <> N'Chờ đặt cọc' OR GiuChoDen > SYSDATETIME())
                  AND NgayGio > DATEADD(HOUR, -2, @GioHen)
                  AND NgayGio < DATEADD(HOUR, 2, @GioHen)
            )
                THROW 50003, N'Bàn mới đã có phiếu Web trùng khung giờ.', 1;

            UPDATE dbo.Web_DatBan
            SET KhuVucId = @KhuVucMoi,
                NgayCapNhat = SYSDATETIME()
            WHERE MaDatBan = @Ma;
            """);
    }

    private void BtnSaveOrder_Click(object? sender, EventArgs e)
    {
        if (currentSelectedTable?.Tag is not Ban ban)
        {
            MessageBox.Show("Vui lòng chọn Bàn trước!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (dgvOrderDetails.Rows.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn ít nhất 1 món ăn trước khi gửi bếp / lưu tạm!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using var db = Program.NewDb();
            var don = LuuDonHangCuaBan(db, ban, TrangThaiDon.DaXacNhan, "Đang dùng");
            LoadDataFromDb();
            lblTableStatus.Text = $"📋 {ban.TenBan} — Đơn #{don.Id} (Đã xác nhận)";
            MessageBox.Show("Đã lưu món. Bạn có thể mở lại bàn để thêm món hoặc thanh toán.");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi lưu tạm đơn hàng: " + ex.GetBaseException().Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static decimal? TinhGiamVoucherTaiQuay(string ma, decimal tienMon)
    {
        using var db = Program.NewDb();

        var homNay = DateTime.Today;
        var voucher = db.WebUuDais.AsNoTracking().SingleOrDefault(x =>
            x.Ma == ma
            && x.HoatDong
            && (x.HanDung == null || x.HanDung >= homNay));

        if (voucher == null || tienMon <= 0)
            return null;

        return Math.Round(Math.Min(
            tienMon * voucher.PhanTramGiam / 100m,
            voucher.GiamToiDa ?? decimal.MaxValue));
    }

    private static bool CoMonDatTruoc(QLNH.Data.AppDbContext db, string? maDatBan)
    {
        if (string.IsNullOrWhiteSpace(maDatBan))
            return false;

        return db.Database.SqlQuery<int>($"""
            SELECT CASE WHEN EXISTS (
                SELECT 1
                FROM dbo.Web_DatBan AS d
                JOIN dbo.Web_DatBanMon AS m ON m.DatBanId = d.Id
                WHERE d.MaDatBan = {maDatBan}
                  AND m.SoLuong > 0
            ) THEN 1 ELSE 0 END AS [Value]
            """).Single() == 1;
    }

    private void BtnPay_Click(object? sender, EventArgs e)
    {
        if (currentSelectedTable?.Tag is not Ban ban || dgvOrderDetails.Rows.Count == 0)
        {
            MessageBox.Show("Không có dữ liệu hóa đơn để thanh toán!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        decimal totalAmount = TinhTongHoaDon();
        string tenBan = ban.TenBan;

        using (var dbKiemTra = Program.NewDb())
        {
            var loi = KiemTraGiuBan(dbKiemTra, ban.Id, _currentOpenOrderId);
            if (loi != null)
            {
                MessageBox.Show(loi, "Bàn đang được giữ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
        }

        string? maDaAp = null;
        decimal giamDaAp = 0;
        bool duDieuKienApMa = false;

        if (_currentOpenOrderId is int idDangMo)
        {
            using var dbDoc = Program.NewDb();
            var donDangMo = dbDoc.DonHangs.AsNoTracking().SingleOrDefault(d => d.Id == idDangMo);
            if (donDangMo == null)
            {
                MessageBox.Show("Đơn đã thay đổi. Vui lòng chọn lại bàn.");
                return;
            }

            maDaAp = donDangMo.MaUuDai;
            giamDaAp = donDangMo.GiamGia;
            duDieuKienApMa = CoMonDatTruoc(dbDoc, donDangMo.MaDatBan);
        }

        var tienMon = TinhTongMon();

        using var paymentForm = new PaymentForm(
            totalAmount,
            tenBan,
            _currentDeposit,
            maDaAp,
            giamDaAp,
            duDieuKienApMa ? ma => TinhGiamVoucherTaiQuay(ma, tienMon) : null);

        if (paymentForm.ShowDialog() != DialogResult.OK) return;

        try
        {
            using var db = Program.NewDb();
            using var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable);

            if (_currentOpenOrderId is int idCanLuu)
            {
                var hienTai = db.DonHangs.AsNoTracking().SingleOrDefault(d => d.Id == idCanLuu);
                if (hienTai == null
                    || hienTai.TrangThai == TrangThaiDon.DaThanhToan
                    || hienTai.TrangThai == TrangThaiDon.DaHuy
                    || hienTai.TrangThai == "Đã huỷ"
                    || hienTai.TrangThai == TrangThaiDon.HoanThanh
                    || hienTai.MaUuDai != maDaAp
                    || hienTai.GiamGia != giamDaAp)
                {
                    throw new InvalidOperationException("Đơn hoặc mã ưu đãi đã thay đổi. Hãy mở lại đơn trước khi thanh toán.");
                }
            }

            var don = LuuDonHangCuaBan(db, ban, TrangThaiDon.DaThanhToan, "Trống");

            if (paymentForm.MaUuDaiMoi is string maMoi)
            {
                if (!string.IsNullOrWhiteSpace(don.MaUuDai))
                    throw new InvalidOperationException("Hóa đơn này đã có mã ưu đãi.");

                if (!CoMonDatTruoc(db, don.MaDatBan))
                    throw new InvalidOperationException("Mã ưu đãi chỉ áp dụng khi đặt bàn kèm món ăn.");

                don.MaUuDai = maMoi;
                don.GiamGia = paymentForm.GiamGiaMoi;
                don.TongTien = paymentForm.TongSauUuDai;

                if (don.MaDatBan is string maDatBan
                    && (maDatBan.StartsWith("HV-", StringComparison.OrdinalIgnoreCase)
                        || maDatBan.StartsWith("AURA-", StringComparison.OrdinalIgnoreCase)))
                {
                    var conLai = Math.Max(0, don.TongTien - _currentDeposit);
                    var soDong = db.Database.ExecuteSqlInterpolated($"""
                        UPDATE dbo.Web_DatBan
                        SET MaUuDai = {maMoi},
                            GiamGia = {don.GiamGia},
                            TongHoaDon = {don.TongTien},
                            ConLai = {conLai},
                            NgayCapNhat = SYSDATETIME()
                        WHERE MaDatBan = {maDatBan}
                          AND NULLIF(LTRIM(RTRIM(MaUuDai)), N'') IS NULL
                        """);

                    if (soDong != 1)
                        throw new InvalidOperationException("Phiếu Web đã có mã hoặc không tìm thấy phiếu liên kết.");
                }

                db.SaveChanges();
            }

            new OrderService(db).DoiTrangThai(don.Id, TrangThaiDon.DaThanhToan);
            tx.Commit();

            PrintBill(tenBan, don.TongTien, _currentDeposit, dgvOrderDetails.Rows);
            _selectedBanId = null;
            _currentOpenOrderId = null;
            _currentDeposit = 0;
            _currentExtraFee = 0;
            lblTableStatus.Text = "📋 Chưa chọn bàn";
            dgvOrderDetails.Rows.Clear();
            UpdateTotal();
            LoadDataFromDb();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Lỗi khi lưu dữ liệu: " + ex.GetBaseException().Message, "Lỗi DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void PrintBill(string tenBan, decimal tongTien, decimal tienCoc, DataGridViewRowCollection rows)
    {
        using var pd = new PrintDocument();
        pd.PrintPage += (_, e) =>
        {
            Graphics g = e.Graphics!;
            using var fontTitle = new Font("Segoe UI", 16, FontStyle.Bold);
            using var fontSub = new Font("Segoe UI", 12, FontStyle.Bold);
            using var fontHeader = new Font("Segoe UI", 11, FontStyle.Bold);
            using var fontNormal = new Font("Segoe UI", 11, FontStyle.Regular);
            using var formatRight = new StringFormat { Alignment = StringAlignment.Far };
            using var formatCenter = new StringFormat { Alignment = StringAlignment.Center };

            int y = 20;
            int pageCenter = e.PageBounds.Width / 2;

            g.DrawString("NHÀ HÀNG HƯƠNG VIỆT", fontTitle, Brushes.Black, new PointF(pageCenter, y), formatCenter);
            y += 38;
            g.DrawString("HÓA ĐƠN THANH TOÁN", fontSub, Brushes.Black, new PointF(pageCenter, y), formatCenter);
            y += 36;
            g.DrawString($"Bàn: {tenBan}   |   Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm}", fontNormal, Brushes.Black, new PointF(20, y));
            y += 30;
            g.DrawString(new string('-', 115), fontNormal, Brushes.Black, new PointF(20, y));
            y += 20;

            int colTenMon = 20;
            int colSL = 430;
            int colGia = 580;
            int colThanhTien = 750;

            g.DrawString("Tên món", fontHeader, Brushes.Black, new PointF(colTenMon, y));
            g.DrawString("SL", fontHeader, Brushes.Black, new PointF(colSL, y), formatRight);
            g.DrawString("Đơn giá", fontHeader, Brushes.Black, new PointF(colGia, y), formatRight);
            g.DrawString("Thành tiền", fontHeader, Brushes.Black, new PointF(colThanhTien, y), formatRight);
            y += 30;
            g.DrawString(new string('-', 115), fontNormal, Brushes.Black, new PointF(20, y));
            y += 20;

            foreach (DataGridViewRow row in rows)
            {
                g.DrawString(row.Cells["TenMon"].Value.ToString()!, fontNormal, Brushes.Black, new PointF(colTenMon, y));
                g.DrawString(row.Cells["SoLuong"].Value.ToString()!, fontNormal, Brushes.Black, new PointF(colSL, y), formatRight);
                g.DrawString(row.Cells["Gia"].Value.ToString()!, fontNormal, Brushes.Black, new PointF(colGia, y), formatRight);
                g.DrawString(row.Cells["ThanhTien"].Value.ToString()!, fontNormal, Brushes.Black, new PointF(colThanhTien, y), formatRight);
                y += 30;
            }

            g.DrawString(new string('-', 115), fontNormal, Brushes.Black, new PointF(20, y));
            y += 28;
            g.DrawString($"TỔNG HÓA ĐƠN: {tongTien:N0} đ", fontSub, Brushes.Black, new PointF(colThanhTien, y), formatRight);
            y += 30;

            if (tienCoc > 0)
            {
                decimal conPhaiThu = Math.Max(0, tongTien - tienCoc);
                g.DrawString($"Đã đặt cọc trước: - {tienCoc:N0} đ", fontHeader, Brushes.SeaGreen, new PointF(colThanhTien, y), formatRight);
                y += 30;
                g.DrawString($"CÒN PHẢI THANH TOÁN: {conPhaiThu:N0} đ", fontTitle, Brushes.Firebrick, new PointF(colThanhTien, y), formatRight);
            }
            else
            {
                g.DrawString($"THÀNH TIỀN THANH TOÁN: {tongTien:N0} đ", fontTitle, Brushes.Firebrick, new PointF(colThanhTien, y), formatRight);
            }
        };

        using var preview = new PrintPreviewDialog
        {
            Document = pd,
            Width = 850,
            Height = 700,
            StartPosition = FormStartPosition.CenterScreen
        };
        preview.ShowDialog();
    }
}
