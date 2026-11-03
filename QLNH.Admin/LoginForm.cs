// LoginForm.cs: Cửa sổ Đăng nhập hệ thống (xác thực tài khoản admin / nhanvien).
using QLNH.Data.Entities;
using QLNH.Data.Services;

namespace QLNH.Admin;

public class LoginForm : Form
{
    private readonly TextBox txtUser = new() { Width = 220 };
    private readonly TextBox txtPass = new() { Width = 220, UseSystemPasswordChar = true };
    private readonly Label lblErr = new() { ForeColor = Color.Firebrick, AutoSize = true };
    private int soLanSai;
    public TaiKhoan? NguoiDung { get; private set; }

    public LoginForm()
    {
        Text = "Đăng nhập - Quản lý nhà hàng";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
        ClientSize = new Size(360, 230);

        var title = new Label { Text = "🍜 HƯƠNG VIỆT", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.Firebrick, AutoSize = true, Left = 100, Top = 15 };
        var btn = new Button { Text = "Đăng nhập", Width = 220, Height = 34, BackColor = Color.Firebrick, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        btn.Click += (_, _) => DangNhap();
        AcceptButton = btn;

        var passPanel = new Panel { Width = 220, Height = 26, Margin = new Padding(3) };
        txtPass.Width = 186;
        txtPass.Location = new Point(0, 0);
        var btnEye = new Button
        {
            Text = "",
            Width = 32,
            Height = txtPass.Height,
            Location = new Point(188, 0),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            TabStop = false
        };
        btnEye.FlatAppearance.BorderColor = Color.LightGray;
        btnEye.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var pen = new Pen(Color.FromArgb(80, 80, 80), 1.6f)
            {
                StartCap = System.Drawing.Drawing2D.LineCap.Round,
                EndCap = System.Drawing.Drawing2D.LineCap.Round
            };
            int cx = btnEye.Width / 2;
            int cy = btnEye.Height / 2;

            // Vẽ viền con mắt
            var ptsTop = new[] { new PointF(cx - 9, cy), new PointF(cx, cy - 6), new PointF(cx + 9, cy) };
            var ptsBot = new[] { new PointF(cx - 9, cy), new PointF(cx, cy + 6), new PointF(cx + 9, cy) };
            g.DrawCurve(pen, ptsTop, 0.6f);
            g.DrawCurve(pen, ptsBot, 0.6f);

            // Vẽ con ngươi
            g.DrawEllipse(pen, cx - 2.5f, cy - 2.5f, 5f, 5f);

            // Khi mật khẩu đang ở dạng chấm (ẩn) -> vẽ đường gạch chéo qua mắt
            if (txtPass.UseSystemPasswordChar)
            {
                g.DrawLine(pen, cx - 8, cy + 6, cx + 8, cy - 6);
            }
        };
        btnEye.Click += (_, _) =>
        {
            txtPass.UseSystemPasswordChar = !txtPass.UseSystemPasswordChar;
            btnEye.Invalidate();
        };
        passPanel.Controls.Add(txtPass);
        passPanel.Controls.Add(btnEye);

        var table = new TableLayoutPanel { Left = 20, Top = 60, Width = 320, Height = 150, ColumnCount = 2 };
        table.Controls.Add(new Label { Text = "Tài khoản", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        table.Controls.Add(txtUser, 1, 0);
        table.Controls.Add(new Label { Text = "Mật khẩu", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        table.Controls.Add(passPanel, 1, 1);
        table.Controls.Add(btn, 1, 2);
        table.Controls.Add(lblErr, 1, 3);
        Controls.AddRange(new Control[] { title, table });
    }

    private void DangNhap()
    {
        lblErr.Text = "";
        if (string.IsNullOrWhiteSpace(txtUser.Text) || string.IsNullOrEmpty(txtPass.Text))
        { lblErr.Text = "Vui lòng nhập đủ tài khoản và mật khẩu."; return; }
        try
        {
            using var db = Program.NewDb();
            var tk = new AuthService(db).DangNhap(txtUser.Text.Trim(), txtPass.Text);
            if (tk == null)
            {
                soLanSai++;
                lblErr.Text = "Sai tài khoản hoặc mật khẩu.";
                if (soLanSai >= 5) { MessageBox.Show("Nhập sai quá 5 lần. Ứng dụng sẽ đóng.", "Cảnh báo"); DialogResult = DialogResult.Cancel; }
                return;
            }
            NguoiDung = tk;
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không kết nối được SQL Server:\n" + ex.GetBaseException().Message, "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
