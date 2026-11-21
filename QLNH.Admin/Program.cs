using Microsoft.Extensions.Configuration;
using QLNH.Data;

namespace QLNH.Admin;

internal static class Program
{
    public static string ConnectionString { get; private set; } = "";
    public static bool IsCloudMode { get; private set; } = false;

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        var cfg = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        IsCloudMode = bool.TryParse(cfg["UseCloudDatabase"], out var c) && c;
        ConnectionString = IsCloudMode
            ? (cfg.GetConnectionString("Cloud") ?? cfg.GetConnectionString("Default")!)
            : (cfg.GetConnectionString("Local") ?? cfg.GetConnectionString("Default")!);

        try
        {
            using var db = NewDb();
            db.EnsureSchemaColumns();
        }
        catch
        {
        }

        while (true)
        {
            using var login = new LoginForm();
            if (login.ShowDialog() != DialogResult.OK || login.NguoiDung == null) return;
            using var main = new MainForm(login.NguoiDung);
            Application.Run(main);
            if (!main.DangXuat) return;
        }
    }

    public static AppDbContext NewDb() => AppDbContext.Create(ConnectionString);
}
