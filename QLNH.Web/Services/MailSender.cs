using System.Net;
using System.Net.Mail;

namespace QLNH_Web.Services;

public interface IMailSender
{
    /// <summary>Trả về true nếu email đã được gửi đi.</summary>
    Task<bool> SendAsync(string to, string subject, string htmlBody);
}

// Cấu hình trong appsettings.json (bỏ trống Host thì chỉ ghi log, không gửi):
// "Smtp": { "Host": "smtp.gmail.com", "Port": 587, "User": "...", "Password": "mật-khẩu-ứng-dụng", "From": "..." }
public class SmtpMailSender : IMailSender
{
    private readonly IConfiguration _cfg;
    private readonly ILogger<SmtpMailSender> _log;
    public SmtpMailSender(IConfiguration cfg, ILogger<SmtpMailSender> log) { _cfg = cfg; _log = log; }

    public async Task<bool> SendAsync(string to, string subject, string htmlBody)
    {
        var host = _cfg["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            _log.LogWarning("Chưa cấu hình Smtp:Host nên không gửi email tới {To}", to);
            return false;
        }
        try
        {
            var port = int.TryParse(_cfg["Smtp:Port"], out var p) ? p : 587;
            using var client = new SmtpClient(host, port) { EnableSsl = true };
            var user = _cfg["Smtp:User"];
            if (!string.IsNullOrEmpty(user)) client.Credentials = new NetworkCredential(user, _cfg["Smtp:Password"]);
            var from = _cfg["Smtp:From"] ?? user ?? "no-reply@localhost";
            using var msg = new MailMessage(from, to, subject, htmlBody) { IsBodyHtml = true };
            await client.SendMailAsync(msg);
            return true;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Gửi email tới {To} thất bại", to);
            return false;
        }
    }
}
