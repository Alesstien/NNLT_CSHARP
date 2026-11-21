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
    // lblTongVal và lblCanThuVal cần truy cập khi cập nhật sau khi áp voucher
    private Label lblTongVal = null!;
    private Label lblCanThuVal = null!;

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
        bool coVoucher = !string.IsNullOrWhiteSpace(_maDaAp) || _tinhGiamGia != null;
        int formHeight = coVoucher ? 480 : 420;

        this.Text = $"Thanh toán - {tenBan}";
        this.Size = new Size(440, formHeight);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.White;
        this.Font = new Font("Segoe UI", 10F);

        // ── Header ──────────────────────────────────────────────
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = Color.FromArgb(26, 115, 232)
        };
        var lblTitle = new Label
        {
            Text = "CHI TIẾT THANH TOÁN",
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        pnlHeader.Controls.Add(lblTitle);

        // ── Các dòng thông tin ───────────────────────────────────
        int col1 = 30, col2 = 215;
        int y = 72;

        var lblTongText = MakeLabel("Tổng hóa đơn:", col1, y, 11.5F);
        lblTongVal = MakeLabel($"{_tongHoaDon:N0} đ", col2, y, 12F, FontStyle.Bold);
        y += 38;

        var lblCocText = MakeLabel("Đã đặt cọc trước:", col1, y, 11.5F);
        var lblCocVal = MakeLabel($"- {_tienCoc:N0} đ", col2, y, 12F, FontStyle.Bold);
        lblCocVal.ForeColor = Color.SeaGreen;
        y += 38;

        // Đường kẻ ngang phân cách
        var sep = new Panel { Location = new Point(col1, y), Size = new Size(370, 1), BackColor = Color.LightGray };
        y += 10;

        var lblCanThuText = MakeLabel("Còn phải thu:", col1, y, 12F, FontStyle.Bold);
        lblCanThuVal = MakeLabel($"{_canThu:N0} đ", col2, y - 3, 15F, FontStyle.Bold);
        lblCanThuVal.ForeColor = Color.Firebrick;
        y += 48;

        var lblKhachDuaText = MakeLabel("Tiền khách đưa:", col1, y, 11.5F);
        txtTienKhachDua = new TextBox
        {
            Font = new Font("Segoe UI", 13F),
            Location = new Point(col2, y - 3),
            Width = 185,
            Text = _canThu.ToString("0"),
            BorderStyle = BorderStyle.FixedSingle
        };
        txtTienKhachDua.TextChanged += TxtTienKhachDua_TextChanged;
        txtTienKhachDua.KeyPress += TxtTienKhachDua_KeyPress;
        y += 44;

        var lblTienThuaText = MakeLabel("Tiền thối lại:", col1, y, 11.5F);
        lblTienThua = MakeLabel("0 đ", col2, y, 13F, FontStyle.Bold);
        lblTienThua.ForeColor = Color.MediumSeaGreen;
        y += 44;

        // ── Nút XÁC NHẬN ───────────────────────────────────────
        int btnY = coVoucher ? y + 68 : y;
        btnXacNhan = new Button
        {
            Text = "XÁC NHẬN THANH TOÁN (ENTER)",
            Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
            BackColor = Color.FromArgb(52, 168, 83),
            ForeColor = Color.White,
            Location = new Point(col1, btnY),
            Size = new Size(370, 50),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnXacNhan.FlatAppearance.BorderSize = 0;
        btnXacNhan.FlatAppearance.MouseOverBackColor = Color.FromArgb(39, 142, 65);
        btnXacNhan.FlatAppearance.MouseDownBackColor = Color.FromArgb(27, 115, 48);
        btnXacNhan.Click += BtnXacNhan_Click;

        this.Controls.AddRange(new Control[]
        {
            pnlHeader,
            lblTongText, lblTongVal,
            lblCocText, lblCocVal,
            sep,
            lblCanThuText, lblCanThuVal,
            lblKhachDuaText, txtTienKhachDua,
            lblTienThuaText, lblTienThua,
            btnXacNhan
        });

        // ── Phần voucher (nếu có) ───────────────────────────────
        if (coVoucher)
        {
            // Đường kẻ ngang trước voucher
            var sepVoucher = new Panel
            {
                Location = new Point(col1, y - 4),
                Size = new Size(370, 1),
                BackColor = Color.LightGray
            };

            var lblVoucher = new Label
            {
                Text = string.IsNullOrWhiteSpace(_maDaAp)
                    ? "🎫  Mã ưu đãi"
                    : $"✅  Đã áp dụng: {_maDaAp}  |  Giảm {_giamDaAp:N0} đ",
                Location = new Point(col1, y + 4),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = string.IsNullOrWhiteSpace(_maDaAp)
                    ? Color.FromArgb(70, 70, 70)
                    : Color.SeaGreen
            };

            Controls.Add(sepVoucher);
            Controls.Add(lblVoucher);

            if (string.IsNullOrWhiteSpace(_maDaAp) && _tinhGiamGia != null)
            {
                int inputY = y + 30;

                var txtVoucher = new TextBox
                {
                    Location = new Point(col1, inputY),
                    Width = 235,
                    Font = new Font("Segoe UI", 11F),
                    MaxLength = 30,
                    CharacterCasing = CharacterCasing.Upper,
                    PlaceholderText = "Nhập mã ưu đãi..."
                };

                var btnApDung = new Button
                {
                    Text = "Áp dụng",
                    Location = new Point(col1 + 240, inputY - 1),
                    Size = new Size(130, 32),
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    BackColor = Color.FromArgb(52, 168, 83),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                btnApDung.FlatAppearance.BorderSize = 0;
                btnApDung.FlatAppearance.MouseOverBackColor = Color.FromArgb(39, 142, 65);
                btnApDung.FlatAppearance.MouseDownBackColor = Color.FromArgb(27, 115, 48);

                btnApDung.Click += (_, _) =>
                {
                    var ma = txtVoucher.Text.Trim().ToUpperInvariant();
                    if (ma.Length == 0)
                    {
                        MessageBox.Show("Hãy nhập mã ưu đãi.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    try
                    {
                        var giam = _tinhGiamGia(ma);
                        if (giam is not decimal soTien || soTien <= 0)
                        {
                            MessageBox.Show("Mã không hợp lệ hoặc đơn không đủ điều kiện.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        MaUuDaiMoi = ma;
                        GiamGiaMoi = Math.Min(soTien, _tongHoaDon);
                        _tongHoaDon -= GiamGiaMoi;
                        _canThu = Math.Max(0, _tongHoaDon - _tienCoc);

                        lblTongVal.Text = $"{_tongHoaDon:N0} đ";
                        lblCanThuVal.Text = $"{_canThu:N0} đ";
                        txtTienKhachDua.Text = _canThu.ToString("0");
                        lblVoucher.Text = $"✅  Đã áp dụng: {ma}  |  Giảm {GiamGiaMoi:N0} đ";
                        lblVoucher.ForeColor = Color.SeaGreen;

                        txtVoucher.Enabled = false;
                        btnApDung.Enabled = false;
                        btnApDung.BackColor = Color.FromArgb(180, 180, 180);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Không kiểm tra được mã: " + ex.GetBaseException().Message,
                            "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                Controls.Add(txtVoucher);
                Controls.Add(btnApDung);
            }
        }

        this.AcceptButton = btnXacNhan;
    }

    // Helper tạo Label nhanh
    private static Label MakeLabel(string text, int x, int y, float fontSize, FontStyle style = FontStyle.Regular)
    {
        return new Label
        {
            Text = text,
            Font = new Font("Segoe UI", fontSize, style),
            Location = new Point(x, y),
            AutoSize = true
        };
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
