//Program.cs: Điểm khởi chạy chính của ứng dụng WinForms, chịu trách nhiệm kích hoạt giao diện đăng nhập khi mở phần mềm.

using Microsoft.Extensions.Configuration;
using QLNH.Data;

namespace QLNH.Admin;

internal static class Program
{
    public static string ConnectionString { get; private set; } = "";

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        var cfg = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory)
                      .AddJsonFile("appsettings.json", optional: false).Build();
        ConnectionString = cfg.GetConnectionString("Default")!;

        while (true)
        {
            using var login = new LoginForm();
            if (login.ShowDialog() != DialogResult.OK || login.NguoiDung == null) return;
            using var main = new MainForm(login.NguoiDung);
            Application.Run(main);
            if (!main.DangXuat) return;   // đóng cửa sổ = thoát; "Đăng xuất" = quay lại đăng nhập
        }
    }

    public static AppDbContext NewDb() => AppDbContext.Create(ConnectionString);
}
