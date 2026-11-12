using System;
using System.Drawing;
using System.Windows.Forms;

namespace QLNH.Admin;

public class PaymentForm : Form
{
    private decimal _tongHoaDon;
    private readonly decimal _tienCoc;
    private decimal _canThu;
    private readonly string? _maDaAp;
    private readonly decimal _giamDaAp;
    private readonly Func<string, decimal?>? _tinhGiamGia;

    public string? MaUuDaiMoi { get; private set; }
    public decimal GiamGiaMoi { get; private set; }
    public decimal TongSauUuDai => _tongHoaDon;

    private TextBox txtTienKhachDua = null!;
    private Label lblTienThua = null!;
    private Button btnXacNhan = null!;

    public PaymentForm(
        decimal tongHoaDon,
        string tenBan,
        decimal tienCoc = 0,
        string? maDaAp = null,
        decimal giamDaAp = 0,
        Func<string, decimal?>? tinhGiamGia = null)
    {
        _tongHoaDon = tongHoaDon;
        _tienCoc = tienCoc;
        _canThu = Math.Max(0, tongHoaDon - tienCoc);
        _maDaAp = maDaAp;
        _giamDaAp = giamDaAp;
        _tinhGiamGia = tinhGiamGia;

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

        var lblTitle = new Label { Text = "CHI TIẾT THANH TOÁN", Font = new Font("Segoe UI", 14F, FontStyle.Bold), Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleCenter, Height = 48 };

        var lblTongText = new Label { Text = "Tổng hóa đơn:", Font = new Font("Segoe UI", 11.5F), Location = new Point(30, 62), AutoSize = true };
        var lblTongVal = new Label { Text = $"{_tongHoaDon:N0} đ", Font = new Font("Segoe UI", 12F, FontStyle.Bold), Location = new Point(200, 60), AutoSize = true };

        var lblCocText = new Label { Text = "Đã đặt cọc trước:", Font = new Font("Segoe UI", 11.5F), Location = new Point(30, 98), AutoSize = true };
        var lblCocVal = new Label { Text = $"- {_tienCoc:N0} đ", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.SeaGreen, Location = new Point(200, 96), AutoSize = true };

        var lblCanThuText = new Label { Text = "Còn phải thu:", Font = new Font("Segoe UI", 12F, FontStyle.Bold), Location = new Point(30, 136), AutoSize = true };
        var lblCanThuVal = new Label { Text = $"{_canThu:N0} đ", Font = new Font("Segoe UI", 15F, FontStyle.Bold), ForeColor = Color.Firebrick, Location = new Point(200, 132), AutoSize = true };

        var lblKhachDuaText = new Label { Text = "Tiền khách đưa:", Font = new Font("Segoe UI", 11.5F), Location = new Point(30, 184), AutoSize = true };
        txtTienKhachDua = new TextBox { Font = new Font("Segoe UI", 13F), Location = new Point(200, 180), Width = 180, Text = _canThu.ToString("0") };
        txtTienKhachDua.TextChanged += TxtTienKhachDua_TextChanged;
        txtTienKhachDua.KeyPress += TxtTienKhachDua_KeyPress;

        var lblTienThuaText = new Label { Text = "Tiền thối lại:", Font = new Font("Segoe UI", 11.5F), Location = new Point(30, 232), AutoSize = true };
        lblTienThua = new Label { Text = "0 đ", Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = Color.MediumSeaGreen, Location = new Point(200, 228), AutoSize = true };

        btnXacNhan = new Button
        {
            Text = "XÁC NHẬN THANH TOÁN (ENTER)",
            Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
            BackColor = Color.MediumSeaGreen,
            ForeColor = Color.White,
            Location = new Point(30, 285),
            Size = new Size(350, 50),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
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

        if (!string.IsNullOrWhiteSpace(_maDaAp) || _tinhGiamGia != null)
        {
            ClientSize = new Size(430, 420);
            btnXacNhan.Top = 345;

            var lblVoucher = new Label
            {
                Text = string.IsNullOrWhiteSpace(_maDaAp)
                    ? "Mã ưu đãi"
                    : $"Đã áp dụng {_maDaAp} · Giảm {_giamDaAp:N0} đ",
                Location = new Point(30, 270),
                AutoSize = true
            };
            Controls.Add(lblVoucher);

            if (string.IsNullOrWhiteSpace(_maDaAp) && _tinhGiamGia != null)
            {
                var txtVoucher = new TextBox
                {
                    Location = new Point(30, 293),
                    Width = 230,
                    MaxLength = 30,
                    CharacterCasing = CharacterCasing.Upper
                };

                var btnApDung = new Button
                {
                    Text = "Áp dụng",
                    Location = new Point(270, 291),
                    Width = 110
                };

                btnApDung.Click += (_, _) =>
                {
                    var ma = txtVoucher.Text.Trim().ToUpperInvariant();
                    if (ma.Length == 0)
                    {
                        MessageBox.Show("Hãy nhập mã ưu đãi.");
                        return;
                    }

                    try
                    {
                        var giam = _tinhGiamGia(ma);
                        if (giam is not decimal soTien || soTien <= 0)
                        {
                            MessageBox.Show("Mã không hợp lệ hoặc đơn không đủ điều kiện.");
                            return;
                        }

                        MaUuDaiMoi = ma;
                        GiamGiaMoi = Math.Min(soTien, _tongHoaDon);
                        _tongHoaDon -= GiamGiaMoi;
                        _canThu = Math.Max(0, _tongHoaDon - _tienCoc);

                        lblTongVal.Text = $"{_tongHoaDon:N0} đ";
                        lblCanThuVal.Text = $"{_canThu:N0} đ";
                        txtTienKhachDua.Text = _canThu.ToString("0");
                        lblVoucher.Text = $"Đã áp dụng {ma} · Giảm {GiamGiaMoi:N0} đ";

                        txtVoucher.Enabled = false;
                        btnApDung.Enabled = false;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Không kiểm tra được mã: " + ex.GetBaseException().Message);
                    }
                };

                Controls.Add(txtVoucher);
                Controls.Add(btnApDung);
            }
        }

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
