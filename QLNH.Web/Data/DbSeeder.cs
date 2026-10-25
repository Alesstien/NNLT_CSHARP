using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QLNH_Web.Controllers;
using QLNH_Web.Models;
using QLNH_Web.Services;

namespace QLNH_Web.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AuthDbContext db, UserManager<AppUser> users, IConfiguration cfg, DatBanService? svc = null)
    {
        if (!await db.KhuVucs.AnyAsync())
        {
            db.KhuVucs.AddRange(
                new KhuVuc { Ten = "Sảnh Chính Hoàng Gia", PhuThu = 0, SucChua = 60, MoTa = "Không gian ấm cúng, nhạc nền acoustic nhẹ nhàng, đèn chùm pha lê sang trọng." },
                new KhuVuc { Ten = "Phòng VIP Trầm Hương", PhuThu = 500_000, SucChua = 12, MoTa = "Phòng riêng biệt cách âm tuyệt đối, hương trầm dịu nhẹ, phục vụ butler riêng." },
                new KhuVuc { Ten = "Ban Công / Terrace Bầu Trời", PhuThu = 300_000, SucChua = 24, MoTa = "View toàn cảnh thành phố rực rỡ về đêm, không gian mở thoáng đãng." },
                new KhuVuc { Ten = "Bàn Lãng Mạn Sân Vườn", PhuThu = 200_000, SucChua = 16, MoTa = "Góc riêng tư bao quanh bởi cây xanh, nến thơm và hồ nước thư thái." });
        }

        if (!await db.KhuVucs.AnyAsync(k => k.ChoTiec))
        {
            db.KhuVucs.AddRange(
                new KhuVuc { Ten = "Sảnh Tiệc Đại Hoàng Gia", PhuThu = 2_000_000, SucChua = 300, ChoTiec = true, MoTa = "Sảnh tiệc sang trọng với sân khấu lớn, màn hình LED, âm thanh ánh sáng chuẩn quốc tế." },
                new KhuVuc { Ten = "Sảnh Tiệc Sân Vườn Ánh Sao", PhuThu = 1_000_000, SucChua = 150, ChoTiec = true, MoTa = "Không gian tiệc ngoài trời lãng mạn, phù hợp tiệc cưới, sinh nhật và liên hoan." });
        }

        if (!await db.MucThucDons.AnyAsync())
        {
            const string kv = "Khai vị", mc = "Món chính", rv = "Rượu vang & đồ uống", tm = "Tráng miệng";
            db.MucThucDons.AddRange(
                new MucThucDon { ThuTu = 1, DanhMuc = kv, Emoji = "🍲", Ten = "Súp Nấm Truffle Dát Vàng 24K", Gia = 490_000, MoTa = "Súp kem nấm Ý cao cấp kết hợp lá vàng 24K và tinh dầu truffle đen." },
                new MucThucDon { ThuTu = 2, DanhMuc = kv, Emoji = "🦞", Ten = "Gỏi Tôm Hùm Sốt Nấm Truffle", Gia = 950_000, MoTa = "Tôm hùm Nha Trang tươi sống, sốt nấm truffle thơm béo." },
                new MucThucDon { ThuTu = 3, DanhMuc = mc, Emoji = "🥩", Ten = "Thịt Bò Wagyu A5 Nướng Đá Núi Lửa", Gia = 1_850_000, MoTa = "Bò Wagyu Kagoshima Nhật Bản, nướng trên đá núi lửa nóng, dùng kèm rau củ áp chảo." },
                new MucThucDon { ThuTu = 4, DanhMuc = mc, Emoji = "🍖", Ten = "Sườn Cừu Úc Đút Lò Thảo Mộc", Gia = 1_250_000, MoTa = "Sườn cừu Úc ướp hương thảo & tỏi, đút lò chậm, sốt vang đỏ." },
                new MucThucDon { ThuTu = 5, DanhMuc = mc, Emoji = "🐟", Ten = "Cá Hồi Na-uy Áp Chảo Sốt Bơ Chanh", Gia = 780_000, MoTa = "Cá hồi Na-uy da giòn, sò điệp áp chảo, sốt bơ chanh." },
                new MucThucDon { ThuTu = 6, DanhMuc = rv, Emoji = "🍷", Ten = "Rượu Vang Đỏ Château Margaux", Gia = 6_800_000, MoTa = "Dòng vang Bordeaux thượng hạng, hương trái cây đen và gỗ sồi tinh tế." },
                new MucThucDon { ThuTu = 7, DanhMuc = rv, Emoji = "🍾", Ten = "Champagne Dom Pérignon Vintage", Gia = 7_900_000, MoTa = "Champagne Pháp hảo hạng, bọt mịn lâu tan, hương bánh brioche." },
                new MucThucDon { ThuTu = 8, DanhMuc = tm, Emoji = "🍰", Ten = "Bánh Soufflé Sô-cô-la Bỉ & Gelato", Gia = 280_000, MoTa = "Soufflé Valrhona 70% cacao nướng nóng, ăn kèm gelato vani." });
        }
        // Sửa tên món theo thực đơn mẫu (dữ liệu tạo từ bản trước đặt tên khác)
        var wagyu = await db.MucThucDons.FirstOrDefaultAsync(m => m.Ten == "Thịt Bò Wagyu A5 Nướng Đá Muối");
        if (wagyu != null)
        {
            wagyu.Ten = "Thịt Bò Wagyu A5 Nướng Đá Núi Lửa";
            wagyu.MoTa = "Bò Wagyu Kagoshima Nhật Bản, nướng trên đá núi lửa nóng, dùng kèm rau củ áp chảo.";
        }

        if (!await db.UuDais.AnyAsync())
        {
            db.UuDais.AddRange(
                new UuDai { Ma = "WELCOME10", MoTa = "Giảm 10% tổng hóa đơn cho khách mới", PhanTramGiam = 10, GiamToiDa = 500_000 },
                new UuDai { Ma = "AURAVIP", MoTa = "Ưu đãi hội viên VIP: giảm 15% tổng hóa đơn", PhanTramGiam = 15, GiamToiDa = 2_000_000 });
        }
        await db.SaveChangesAsync();

        // Tài khoản quản trị mặc định (đổi mật khẩu sau khi chạy lần đầu, hoặc đặt AdminSeed:Email / AdminSeed:Password trong appsettings.json)
        var email = cfg["AdminSeed:Email"] ?? "admin@aura.vn";
        if (await users.FindByEmailAsync(email) == null)
        {
            var admin = new AppUser { UserName = email, Email = email, EmailConfirmed = true, PhoneNumber = "0900000000", HoTen = "Quản trị viên" };
            var r = await users.CreateAsync(admin, cfg["AdminSeed:Password"] ?? "Admin@123");
            if (r.Succeeded) await users.AddToRoleAsync(admin, AccountController.RoleAdmin);
        }

        // Đồng bộ các đơn đặt bàn và tài khoản khách trên Web sang bảng WinForms (QLNH.Admin)
        if (svc != null)
        {
            var allUsers = await db.Users.AsNoTracking().ToListAsync();
            foreach (var u in allUsers)
            {
                var isAdmin = await users.IsInRoleAsync(u, AccountController.RoleAdmin);
                await svc.DongBoTaiKhoanSangWinFormsAsync(u, null, null, isAdmin);
            }

            var webDatBans = await db.DatBans.Include(d => d.KhuVuc).Include(d => d.Mons)
                .Where(d => d.TrangThai != TrangThaiDatBan.ChoCoc)
                .OrderBy(d => d.Id)
                .ToListAsync();
            foreach (var b in webDatBans)
            {
                await svc.DongBoSangWinFormsAsync(b);
            }
        }
    }
}
