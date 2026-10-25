using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using QLNH.Data.Entities;
using QLNH.Data.Services;

namespace QLNH.Admin
{
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
            Panel wrapTables = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
            wrapTables.Controls.Add(pnlTables);

            pnlMenu = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White, Padding = new Padding(10) };
            Panel wrapMenu = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
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
                Dock = DockStyle.Fill, BackgroundColor = Color.White, AllowUserToAddRows = false,
                ReadOnly = true, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, MultiSelect = false
            };
            dgvOrderDetails.Columns.Add("TenMon", "Tên Món");
            dgvOrderDetails.Columns.Add("SoLuong", "SL");
            dgvOrderDetails.Columns.Add("Gia", "Giá");
            dgvOrderDetails.Columns.Add("ThanhTien", "Tổng");
            dgvOrderDetails.Columns["SoLuong"].Width = 40;

            Panel pnlPaymentArea = new Panel { Dock = DockStyle.Bottom, Height = 172, Padding = new Padding(0, 6, 0, 0) };
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

            btnPay = new Button { Text = "💳 THANH TOÁN & IN HÓA ĐƠN", Font = new Font("Segoe UI", 11.5F, FontStyle.Bold), BackColor = Color.MediumSeaGreen, ForeColor = Color.White, Dock = DockStyle.Bottom, Height = 48, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnPay.FlatAppearance.BorderSize = 0;
            btnPay.Click += BtnPay_Click;

            pnlPaymentArea.Controls.Add(pnlActionRow);
            pnlPaymentArea.Controls.Add(lblTotal);
            pnlPaymentArea.Controls.Add(lblSubTotal);
            pnlPaymentArea.Controls.Add(btnPay);
            pnlBill.Controls.Add(dgvOrderDetails);
            pnlBill.Controls.Add(lblTableStatus);
            pnlBill.Controls.Add(pnlPaymentArea);
            Panel wrapBill = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
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
                foreach (var ban in dsBan)
                {
                    Button btnTable = new Button
                    {
                        Text = $"{ban.TenBan}\n({ban.TrangThai})",
                        Size = new Size(85, 80),
                        Margin = new Padding(5),
                        BackColor = ban.TrangThai == "Trống" ? Color.Gainsboro : Color.Orange,
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
                    Button btnFood = new Button
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

        private void Table_Click(object? sender, EventArgs e)
        {
            if (sender is Button clickedTable && clickedTable.Tag is Ban ban)
            {
                if (currentSelectedTable != null && currentSelectedTable.Tag is Ban oldBan)
                {
                    currentSelectedTable.BackColor = oldBan.TrangThai == "Trống" ? Color.Gainsboro : Color.Orange;
                    currentSelectedTable.ForeColor = Color.Black;
                }

                clickedTable.BackColor = Color.SeaGreen;
                clickedTable.ForeColor = Color.White;
                currentSelectedTable = clickedTable;
                _selectedBanId = ban.Id;

                TaiDonDangMoCuaBan(ban);
            }
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
                var openOrder = db.DonHangs
                    .Include(d => d.ChiTiets)
                    .ThenInclude(c => c.MonAn)
                    .Where(d => d.BanId == ban.Id && d.TrangThai != "Đã thanh toán" && d.TrangThai != "Đã hủy" && d.TrangThai != "Đã huỷ")
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
                    // Giữ lại phần phụ thu khu vực / VAT từ đơn đặt bàn Web (nếu có)
                    if (openOrder.TongTien > sumMon)
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

        private decimal TinhTongHoaDon()
        {
            return TinhTongMon() + _currentExtraFee;
        }

        private void UpdateTotal()
        {
            decimal total = TinhTongHoaDon();
            decimal conLai = Math.Max(0, total - _currentDeposit);
            lblSubTotal.Text = $"Tổng hóa đơn: {total:N0} đ   |   Đã cọc: {_currentDeposit:N0} đ";
            lblTotal.Text = $"Còn phải thu: {conLai:N0} đ";
        }

        private DonHang LuuDonHangCuaBan(QLNH.Data.AppDbContext db, Ban ban, string trangThaiDon, string trangThaiBan)
        {
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

            if (dlg.ShowDialog(this) != DialogResult.OK || cbo.SelectedItem is not Ban Chon) return;

            try
            {
                // Nếu bàn hiện tại có chọn món nhưng chưa lưu đơn, lưu tạm trước khi chuyển
                if (_currentOpenOrderId == null && dgvOrderDetails.Rows.Count > 0)
                {
                    LuuDonHangCuaBan(db, banCu, TrangThaiDon.DangCheBien, "Đang dùng");
                }

                if (_currentOpenOrderId.HasValue)
                {
                    var don = db.DonHangs.Find(_currentOpenOrderId.Value);
                    if (don != null)
                    {
                        don.BanId = Chon.Id;
                        if (don.TenKhach == $"Khách tại bàn ({banCu.TenBan})")
                            don.TenKhach = $"Khách tại bàn ({Chon.TenBan})";
                    }
                }

                var datBans = db.DatBans.Where(d => d.BanId == banCu.Id && d.TrangThai != "Đã đến" && d.TrangThai != "Đã hủy" && d.TrangThai != "Đã huỷ").ToList();
                foreach (var dbItem in datBans)
                {
                    dbItem.BanId = Chon.Id;
                }

                var bCu = db.Bans.Find(banCu.Id);
                var bMoi = db.Bans.Find(Chon.Id);
                if (bCu != null) bCu.TrangThai = "Trống";
                if (bMoi != null) bMoi.TrangThai = "Đang dùng";

                db.SaveChanges();

                _selectedBanId = Chon.Id;
                LoadDataFromDb();
                if (bMoi != null) TaiDonDangMoCuaBan(bMoi);

                MessageBox.Show($"Đã chuyển khách và toàn bộ hóa đơn từ {banCu.TenBan} sang {Chon.TenBan} thành công!", "Đổi bàn thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi đổi bàn: " + ex.GetBaseException().Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                var don = LuuDonHangCuaBan(db, ban, TrangThaiDon.DangCheBien, "Đang dùng");
                LoadDataFromDb();
                lblTableStatus.Text = $"📋 {ban.TenBan} — Đơn #{don.Id} (Đang chế biến)";
                MessageBox.Show($"Đã lưu order của {ban.TenBan} (Đơn #{don.Id}) và chuyển trạng thái bàn sang 'Đang dùng'.\nBạn có thể chuyển sang phục vụ bàn khác và quay lại gọi thêm món bất cứ lúc nào!", "Đã gửi bếp", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi lưu tạm đơn hàng: " + ex.GetBaseException().Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

            using (PaymentForm paymentForm = new PaymentForm(totalAmount, tenBan, _currentDeposit))
            {
                if (paymentForm.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using var db = Program.NewDb();
                        var don = LuuDonHangCuaBan(db, ban, TrangThaiDon.DaThanhToan, "Trống");
                        new OrderService(db).DoiTrangThai(don.Id, TrangThaiDon.DaThanhToan);

                        PrintBill(tenBan, totalAmount, _currentDeposit, dgvOrderDetails.Rows);

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
            }
        }

        private void PrintBill(string tenBan, decimal tongTien, decimal tienCoc, DataGridViewRowCollection rows)
        {
            PrintDocument pd = new PrintDocument();
            pd.PrintPage += (sender, e) =>
            {
                Graphics g = e.Graphics!;
                Font fontTitle = new Font("Segoe UI", 16, FontStyle.Bold);
                Font fontSub = new Font("Segoe UI", 12, FontStyle.Bold);
                Font fontHeader = new Font("Segoe UI", 11, FontStyle.Bold);
                Font fontNormal = new Font("Segoe UI", 11, FontStyle.Regular);

                StringFormat formatRight = new StringFormat { Alignment = StringAlignment.Far };
                StringFormat formatCenter = new StringFormat { Alignment = StringAlignment.Center };

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

            PrintPreviewDialog preview = new PrintPreviewDialog
            {
                Document = pd,
                Width = 850,
                Height = 700,
                StartPosition = FormStartPosition.CenterScreen
            };
            preview.ShowDialog();
        }
    }
}