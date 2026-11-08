using Microsoft.EntityFrameworkCore;
using QLNH.Data;

namespace QLNH_Web.Services;

public sealed class TuHuyDatBanWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<TuHuyDatBanWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await db.Database.ExecuteSqlRawAsync("""
                    SET XACT_ABORT ON;
                    BEGIN TRANSACTION;

                    DECLARE @BayGio datetime2 = SYSDATETIME();
                    DECLARE @HetHan TABLE (MaDatBan nvarchar(30) NOT NULL);

                    UPDATE d
                    SET TrangThai = N'Đã hủy'
                    OUTPUT inserted.MaDatBan INTO @HetHan
                    FROM dbo.DatBan AS d
                    JOIN dbo.Ban AS b ON b.Id = d.BanId
                    WHERE d.TrangThai = N'Đã xác nhận'
                      AND d.BatDauPhucVuLuc IS NULL
                      AND NULLIF(LTRIM(RTRIM(d.MaDatBan)), N'') IS NOT NULL
                      AND d.NgayGio <= DATEADD(MINUTE, -15, @BayGio)
                      AND b.TrangThai IN (N'Trống', N'Đã đặt')
                      AND NOT EXISTS (
                          SELECT 1 FROM dbo.DonHang AS o
                          WHERE o.MaDatBan = d.MaDatBan
                            AND o.TrangThai IN (
                                N'Đang chế biến', N'Hoàn thành', N'Đã thanh toán')
                      )
                      AND NOT EXISTS (
                          SELECT 1 FROM dbo.Web_DatBan AS w
                          WHERE w.MaDatBan = d.MaDatBan
                            AND w.TrangThai IN (
                                N'Đang chế biến', N'Đã hoàn thành', N'Đã thanh toán')
                      );

                    UPDATE o
                    SET TrangThai = N'Đã hủy'
                    FROM dbo.DonHang AS o
                    JOIN @HetHan AS h ON h.MaDatBan = o.MaDatBan
                    WHERE o.TrangThai IN (N'Mới', N'Đã xác nhận');

                    UPDATE w
                    SET TrangThai = N'Đã hủy',
                        LyDoHuy = N'Quá giờ hẹn 15 phút và chưa nhận bàn',
                        NgayCapNhat = @BayGio
                    FROM dbo.Web_DatBan AS w
                    JOIN @HetHan AS h ON h.MaDatBan = w.MaDatBan
                    WHERE w.TrangThai IN (N'Đang chờ duyệt', N'Đã xác nhận');

                    COMMIT TRANSACTION;
                    """, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Không thể kiểm tra phiếu đặt bàn quá giờ.");
            }
        }
    }
}
