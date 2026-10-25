using System;
using System.Drawing;
using System.Windows.Forms;

namespace QLNH.Admin
{
    public class PaymentForm : Form
    {
        private readonly decimal _tongHoaDon;
        private readonly decimal _tienCoc;
        private readonly decimal _canThu;
        private TextBox txtTienKhachDua = null!;
        private Label lblTienThua = null!;
        private Button btnXacNhan = null!;

        public PaymentForm(decimal tongHoaDon, string tenBan, decimal tienCoc = 0)
        {
            _tongHoaDon = tongHoaDon;
            _tienCoc = tienCoc;
            _canThu = Math.Max(0, tongHoaDon - tienCoc);
            InitializeUI(tenBan);
        }

        private void InitializeUI(string tenBan)
        {
            this.Text = $"Thanh toán - {tenBan}";
            this.Size = new Size(430, 410);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;

            Label lblTitle = new Label { Text = "CHI TIẾT THANH TOÁN", Font = new Font("Segoe UI", 14F, FontStyle.Bold), Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleCenter, Height = 48 };

            Label lblTongText = new Label { Text = "Tổng hóa đơn:", Font = new Font("Segoe UI", 11.5F), Location = new Point(30, 62), AutoSize = true };
            Label lblTongVal = new Label { Text = $"{_tongHoaDon:N0} đ", Font = new Font("Segoe UI", 12F, FontStyle.Bold), Location = new Point(200, 60), AutoSize = true };

            Label lblCocText = new Label { Text = "Đã đặt cọc trước:", Font = new Font("Segoe UI", 11.5F), Location = new Point(30, 98), AutoSize = true };
            Label lblCocVal = new Label { Text = $"- {_tienCoc:N0} đ", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.SeaGreen, Location = new Point(200, 96), AutoSize = true };

            Label lblCanThuText = new Label { Text = "Còn phải thu:", Font = new Font("Segoe UI", 12F, FontStyle.Bold), Location = new Point(30, 136), AutoSize = true };
            Label lblCanThuVal = new Label { Text = $"{_canThu:N0} đ", Font = new Font("Segoe UI", 15F, FontStyle.Bold), ForeColor = Color.Firebrick, Location = new Point(200, 132), AutoSize = true };

            Label lblKhachDuaText = new Label { Text = "Tiền khách đưa:", Font = new Font("Segoe UI", 11.5F), Location = new Point(30, 184), AutoSize = true };
            txtTienKhachDua = new TextBox { Font = new Font("Segoe UI", 13F), Location = new Point(200, 180), Width = 180, Text = _canThu.ToString("0") };
            txtTienKhachDua.TextChanged += TxtTienKhachDua_TextChanged;
            txtTienKhachDua.KeyPress += TxtTienKhachDua_KeyPress;

            Label lblTienThuaText = new Label { Text = "Tiền thối lại:", Font = new Font("Segoe UI", 11.5F), Location = new Point(30, 232), AutoSize = true };
            lblTienThua = new Label { Text = "0 đ", Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = Color.MediumSeaGreen, Location = new Point(200, 228), AutoSize = true };

            btnXacNhan = new Button { Text = "XÁC NHẬN THANH TOÁN (ENTER)", Font = new Font("Segoe UI", 11.5F, FontStyle.Bold), BackColor = Color.MediumSeaGreen, ForeColor = Color.White, Location = new Point(30, 285), Size = new Size(350, 50), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnXacNhan.FlatAppearance.BorderSize = 0;
            btnXacNhan.Click += BtnXacNhan_Click;

            this.Controls.AddRange(new Control[]
            {
                lblTitle,
                lblTongText, lblTongVal,
                lblCocText, lblCocVal,
                lblCanThuText, lblCanThuVal,
                lblKhachDuaText, txtTienKhachDua,
                lblTienThuaText, lblTienThua,
                btnXacNhan
            });

            this.AcceptButton = btnXacNhan;
        }

        private void TxtTienKhachDua_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void TxtTienKhachDua_TextChanged(object? sender, EventArgs e)
        {
            if (decimal.TryParse(txtTienKhachDua.Text, out decimal tienKhach))
            {
                decimal tienThua = tienKhach - _canThu;
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
            if (decimal.TryParse(txtTienKhachDua.Text, out decimal tienKhach) && tienKhach >= _canThu)
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