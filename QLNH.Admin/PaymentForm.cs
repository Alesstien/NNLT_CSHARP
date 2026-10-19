using System;
using System.Drawing;
using System.Windows.Forms;

namespace QLNH.Admin
{
    public class PaymentForm : Form
    {
        private decimal _tongTien;
        private TextBox txtTienKhachDua = null!;
        private Label lblTienThua = null!;
        private Button btnXacNhan = null!;

        public PaymentForm(decimal tongTien, string tenBan)
        {
            _tongTien = tongTien;
            InitializeUI(tenBan);
        }

        private void InitializeUI(string tenBan)
        {
            this.Text = $"Thanh toán - {tenBan}";
            this.Size = new Size(400, 350);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;

            Label lblTitle = new Label { Text = "CHI TIẾT THANH TOÁN", Font = new Font("Segoe UI", 14F, FontStyle.Bold), Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleCenter, Height = 50 };
            
            Label lblTongTienText = new Label { Text = "Tổng cộng:", Font = new Font("Segoe UI", 12F), Location = new Point(30, 70), AutoSize = true };
            Label lblTongTienValue = new Label { Text = $"{_tongTien:N0} đ", Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = Color.Firebrick, Location = new Point(180, 65), AutoSize = true };

            Label lblKhachDuaText = new Label { Text = "Tiền khách đưa:", Font = new Font("Segoe UI", 12F), Location = new Point(30, 120), AutoSize = true };
            txtTienKhachDua = new TextBox { Font = new Font("Segoe UI", 14F), Location = new Point(180, 115), Width = 150 };
            txtTienKhachDua.TextChanged += TxtTienKhachDua_TextChanged;
            txtTienKhachDua.KeyPress += TxtTienKhachDua_KeyPress;

            Label lblTienThuaText = new Label { Text = "Tiền thối lại:", Font = new Font("Segoe UI", 12F), Location = new Point(30, 170), AutoSize = true };
            lblTienThua = new Label { Text = "0 đ", Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = Color.MediumSeaGreen, Location = new Point(180, 165), AutoSize = true };

            btnXacNhan = new Button { Text = "XÁC NHẬN (ENTER)", Font = new Font("Segoe UI", 12F, FontStyle.Bold), BackColor = Color.MediumSeaGreen, ForeColor = Color.White, Location = new Point(30, 230), Size = new Size(320, 50), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnXacNhan.FlatAppearance.BorderSize = 0;
            btnXacNhan.Click += BtnXacNhan_Click;

            this.Controls.Add(lblTitle);
            this.Controls.Add(lblTongTienText);
            this.Controls.Add(lblTongTienValue);
            this.Controls.Add(lblKhachDuaText);
            this.Controls.Add(txtTienKhachDua);
            this.Controls.Add(lblTienThuaText);
            this.Controls.Add(lblTienThua);
            this.Controls.Add(btnXacNhan);

            this.AcceptButton = btnXacNhan; // Bấm Enter tự kích hoạt nút này
        }

        // Chỉ cho phép nhập số
        private void TxtTienKhachDua_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        // Tự động tính tiền thừa khi gõ phím
        private void TxtTienKhachDua_TextChanged(object? sender, EventArgs e)
        {
            if (decimal.TryParse(txtTienKhachDua.Text, out decimal tienKhach))
            {
                decimal tienThua = tienKhach - _tongTien;
                lblTienThua.Text = tienThua >= 0 ? $"{tienThua:N0} đ" : "Chưa đủ tiền";
                lblTienThua.ForeColor = tienThua >= 0 ? Color.MediumSeaGreen : Color.Firebrick;
            }
            else
            {
                lblTienThua.Text = "0 đ";
                lblTienThua.ForeColor = Color.Black;
            }
        }

        private void BtnXacNhan_Click(object? sender, EventArgs e)
        {
            if (decimal.TryParse(txtTienKhachDua.Text, out decimal tienKhach) && tienKhach >= _tongTien)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Số tiền khách đưa chưa đủ để thanh toán!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}