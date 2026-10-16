using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;

namespace QLNH.Admin
{
    public partial class PosPanel : UserControl
    {
        private TableLayoutPanel mainLayout = null!;
        private FlowLayoutPanel pnlTables = null!;
        private FlowLayoutPanel pnlMenu = null!;
        private Panel pnlBill = null!;
        private DataGridView dgvOrderDetails = null!;
        private Label lblTotal = null!;
        private Button btnPay = null!;

        // Biến lưu vết bàn đang được chọn
        private Button? currentSelectedTable = null;

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
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F)); 
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F)); 

            pnlTables = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White, Padding = new Padding(10) };
            Panel wrapTables = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
            wrapTables.Controls.Add(pnlTables);

            pnlMenu = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White, Padding = new Padding(10) };
            Panel wrapMenu = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
            wrapMenu.Controls.Add(pnlMenu);

            pnlBill = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10) };
            
            dgvOrderDetails = new DataGridView
            {
                Dock = DockStyle.Fill, BackgroundColor = Color.White, AllowUserToAddRows = false,
                ReadOnly = true, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgvOrderDetails.Columns.Add("TenMon", "Tên Món");
            dgvOrderDetails.Columns.Add("SoLuong", "SL");
            dgvOrderDetails.Columns.Add("Gia", "Giá");
            dgvOrderDetails.Columns.Add("ThanhTien", "Tổng");
            dgvOrderDetails.Columns["SoLuong"].Width = 40;

            Panel pnlPaymentArea = new Panel { Dock = DockStyle.Bottom, Height = 100, Padding = new Padding(0, 10, 0, 0) };
            lblTotal = new Label { Text = "Tổng tiền: 0 đ", Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = Color.Firebrick, Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleRight };
            
            btnPay = new Button { Text = "THANH TOÁN", Font = new Font("Segoe UI", 12F, FontStyle.Bold), BackColor = Color.MediumSeaGreen, ForeColor = Color.White, Dock = DockStyle.Bottom, Height = 50, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnPay.FlatAppearance.BorderSize = 0;
            btnPay.Click += BtnPay_Click;

            pnlPaymentArea.Controls.Add(lblTotal);
            pnlPaymentArea.Controls.Add(btnPay);
            pnlBill.Controls.Add(dgvOrderDetails);
            pnlBill.Controls.Add(pnlPaymentArea);
            Panel wrapBill = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
            wrapBill.Controls.Add(pnlBill);

            mainLayout.Controls.Add(wrapTables, 0, 0);
            mainLayout.Controls.Add(wrapMenu, 1, 0);
            mainLayout.Controls.Add(wrapBill, 2, 0);
            this.Controls.Add(mainLayout);
        }

        private void LoadDataFromDb()
        {
            pnlTables.Controls.Clear();
            pnlMenu.Controls.Clear();

            try
            {
                using var db = Program.NewDb(); 

                var dsBan = db.Set<QLNH.Data.Entities.Ban>().ToList(); 
                foreach (var ban in dsBan)
                {
                    Button btnTable = new Button
                    {
                        Text = $"{ban.TenBan}\n({ban.TrangThai})",
                        Size = new Size(80, 80),
                        Margin = new Padding(5),
                        BackColor = ban.TrangThai == "Trống" ? Color.Gainsboro : Color.Orange,
                        FlatStyle = FlatStyle.Flat,
                        Cursor = Cursors.Hand,
                        Tag = ban
                    };
                    btnTable.FlatAppearance.BorderSize = 0;
                    btnTable.Click += Table_Click;
                    pnlTables.Controls.Add(btnTable);
                }

                var dsMon = db.Set<QLNH.Data.Entities.MonAn>().Where(m => m.ConPhucVu).ToList();
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
            if (sender is Button clickedTable)
            {
                dynamic ban = clickedTable.Tag!; 

                if (currentSelectedTable != null)
                {
                    dynamic oldBan = currentSelectedTable.Tag!;
                    currentSelectedTable.BackColor = oldBan.TrangThai == "Trống" ? Color.Gainsboro : Color.Orange;
                    currentSelectedTable.ForeColor = Color.Black;
                }

                clickedTable.BackColor = Color.SeaGreen;
                clickedTable.ForeColor = Color.White;
                currentSelectedTable = clickedTable;

                dgvOrderDetails.Rows.Clear(); 
                UpdateTotal();
            }
        }

        private void Food_Click(object? sender, EventArgs e)
        {
            if (currentSelectedTable == null)
            {
                MessageBox.Show("Vui lòng chọn Bàn trước khi gọi món!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (sender is Button clickedFood)
            {
                dynamic mon = clickedFood.Tag!;
                string tenMon = mon.TenMon;
                decimal gia = mon.Gia;

                bool daTonTai = false;
                foreach (DataGridViewRow row in dgvOrderDetails.Rows)
                {
                    if (row.Cells["TenMon"].Value.ToString() == tenMon)
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

        private void UpdateTotal()
        {
            decimal total = 0;
            foreach (DataGridViewRow row in dgvOrderDetails.Rows)
            {
                string thanhTienStr = row.Cells["ThanhTien"].Value.ToString()!.Replace(",", "");
                total += Convert.ToDecimal(thanhTienStr);
            }
            lblTotal.Text = $"Tổng tiền: {total:N0} đ";
        }

        private void BtnPay_Click(object? sender, EventArgs e)
        {
            if (currentSelectedTable == null || dgvOrderDetails.Rows.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu hóa đơn để thanh toán!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            decimal totalAmount = 0;
            foreach (DataGridViewRow row in dgvOrderDetails.Rows)
            {
                totalAmount += Convert.ToDecimal(row.Cells["ThanhTien"].Value.ToString()!.Replace(",", ""));
            }

            dynamic ban = currentSelectedTable.Tag!;
            string tenBan = ban.TenBan;

            using (PaymentForm paymentForm = new PaymentForm(totalAmount, tenBan))
            {
                if (paymentForm.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using var db = Program.NewDb();
                        
                        var donHang = new QLNH.Data.Entities.DonHang
                        {
                            TenKhach = $"Khách lẻ ({tenBan})",
                            BanId = ban.Id,
                            TrangThai = "Đã thanh toán",
                            TongTien = totalAmount,
                            NgayTao = DateTime.Now
                        };
                        db.Add(donHang);
                        db.SaveChanges(); 

                        foreach (DataGridViewRow row in dgvOrderDetails.Rows)
                        {
                            string tenMon = row.Cells["TenMon"].Value.ToString()!;
                            var monAn = db.Set<QLNH.Data.Entities.MonAn>().FirstOrDefault(m => m.TenMon == tenMon);
                            
                            if (monAn != null)
                            {
                                var chiTiet = new QLNH.Data.Entities.ChiTietDonHang
                                {
                                    DonHangId = donHang.Id,
                                    MonAnId = monAn.Id,
                                    SoLuong = Convert.ToInt32(row.Cells["SoLuong"].Value),
                                    DonGia = Convert.ToDecimal(row.Cells["Gia"].Value.ToString()!.Replace(",", ""))
                                };
                                db.Add(chiTiet);
                            }
                        }

                        var banUpdate = db.Set<QLNH.Data.Entities.Ban>().Find(ban.Id);
                        if (banUpdate != null)
                        {
                            banUpdate.TrangThai = "Trống";
                            db.Update(banUpdate);
                        }

                        db.SaveChanges();

                        // Gọi hàm in hóa đơn vừa tạo
                        PrintBill(tenBan, totalAmount, dgvOrderDetails.Rows);

                        dgvOrderDetails.Rows.Clear();
                        UpdateTotal();
                        LoadDataFromDb(); 
                        currentSelectedTable = null;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi khi lưu dữ liệu: " + ex.GetBaseException().Message, "Lỗi DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // FIX LỖI CÂN XỨNG: Thêm StringFormat để canh lề phải cho các cột số tiền
        private void PrintBill(string tenBan, decimal tongTien, DataGridViewRowCollection rows)
        {
            PrintDocument pd = new PrintDocument();
            pd.PrintPage += (sender, e) =>
            {
                Graphics g = e.Graphics!;
                Font fontTitle = new Font("Segoe UI", 16, FontStyle.Bold);
                Font fontHeader = new Font("Segoe UI", 11, FontStyle.Bold);
                Font fontNormal = new Font("Segoe UI", 11, FontStyle.Regular);
                
                StringFormat formatRight = new StringFormat { Alignment = StringAlignment.Far };
                StringFormat formatCenter = new StringFormat { Alignment = StringAlignment.Center };

                int y = 20;
                int pageCenter = e.PageBounds.Width / 2;

                g.DrawString("NHÀ HÀNG ĐẠI HỌC SÀI GÒN", fontTitle, Brushes.Black, new PointF(pageCenter, y), formatCenter);
                y += 40;
                g.DrawString("HÓA ĐƠN THANH TOÁN", fontTitle, Brushes.Black, new PointF(pageCenter, y), formatCenter);
                y += 40;
                g.DrawString($"Bàn: {tenBan}   |   Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm}", fontNormal, Brushes.Black, new PointF(20, y));
                y += 30;
                g.DrawString(new string('-', 115), fontNormal, Brushes.Black, new PointF(20, y));
                y += 20;
                
                // Đặt lại các mốc tọa độ X cho các cột
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
                y += 30;
                g.DrawString($"TỔNG CỘNG: {tongTien:N0} đ", fontTitle, Brushes.Firebrick, new PointF(colThanhTien, y), formatRight);
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