using Microsoft.EntityFrameworkCore;
using QLNH.Data;

namespace QLNH_Web.Services;

/// <summary>
/// Tự động hủy phiếu đặt bàn đã xác nhận nhưng khách không đến sau 15 phút.
/// Chỉ giải phóng bàn khi không còn phiếu đặt hoặc đơn hàng khác đang sử dụng bàn.
/// </summary>
public sealed class TuHuyDatBanWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<TuHuyDatBanWorker> logger) : BackgroundService
{
    private static readonly TimeSpan ChuKyKiemTra = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(ChuKyKiemTra);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    await db.Database.ExecuteSqlRawAsync("""
                        SET NOCOUNT ON;
                        SET XACT_ABORT ON;

                        BEGIN TRY
                            BEGIN TRANSACTION;

                            DECLARE @BayGio datetime2(0) = SYSDATETIME();

                            -- Lưu mã đặt bàn và bàn vật lý vừa được tự động hủy.
                            DECLARE @HetHan TABLE
                            (
                                MaDatBan nvarchar() NOT NULL,
                                BanId int NULL
                            );

                            /*
                               1. Hủy phiếu WinForms đã xác nhận, quá giờ hẹn ít nhất 15 phút,
                                  chưa bắt đầu phục vụ và chưa có đơn liên quan đã vào xử lý.

                               Không dùng trạng thái hiện tại của bàn làm điều kiện hủy:
                               bàn có thể đang được một đơn khác sử dụng. Phiếu no-show vẫn
                               cần được hủy, nhưng bàn đang dùng sẽ không bị giải phóng ở bước 4.
                            */
                            UPDATE d
                            SET TrangThai = N'Đã hủy'
                            OUTPUT inserted.MaDatBan, inserted.BanId
                                INTO @HetHan (MaDatBan, BanId)
                            FROM dbo.DatBan AS d WITH (UPDLOCK, ROWLOCK)
                            INNER JOIN dbo.Ban AS b
                                ON b.Id = d.BanId
                            WHERE d.TrangThai = N'Đã xác nhận'
                              AND d.BatDauPhucVuLuc IS NULL
                              AND NULLIF(LTRIM(RTRIM(d.MaDatBan)), N'') IS NOT NULL
                              AND d.NgayGio <= DATEADD(SECOND, -15, @BayGio)
                              AND NOT EXISTS
                              (
                                  SELECT 1
                                  FROM dbo.DonHang AS o WITH (UPDLOCK, HOLDLOCK)
                                  WHERE o.MaDatBan = d.MaDatBan
                                    AND o.TrangThai IN
                                    (
                                        N'Đang chế biến',
                                        N'Hoàn thành',
                                        N'Đã thanh toán'
                                    )
                              )
                              AND NOT EXISTS
                              (
                                  SELECT 1
                                  FROM dbo.Web_DatBan AS w WITH (UPDLOCK, HOLDLOCK)
                                  WHERE w.MaDatBan = d.MaDatBan
                                    AND w.TrangThai IN
                                    (
                                        N'Đang chế biến',
                                        N'Đã hoàn thành',
                                        N'Đã thanh toán'
                                    )
                              );

                            -- 2. Hủy các đơn POS chưa bắt đầu xử lý thuộc phiếu vừa hết hạn.
                            UPDATE o
                            SET TrangThai = N'Đã hủy'
                            FROM dbo.DonHang AS o
                            INNER JOIN
                            (
                                SELECT DISTINCT MaDatBan
                                FROM @HetHan
                            ) AS h ON h.MaDatBan = o.MaDatBan
                            WHERE o.TrangThai IN (N'Mới', N'Đã xác nhận');

                            -- 3. Đồng bộ trạng thái hủy và lý do sang phiếu đặt bàn Website.
                            UPDATE w
                            SET TrangThai = N'Đã hủy',
                                LyDoHuy = N'Quá giờ hẹn 15 phút và chưa nhận bàn',
                                NgayCapNhat = @BayGio
                            FROM dbo.Web_DatBan AS w
                            INNER JOIN
                            (
                                SELECT DISTINCT MaDatBan
                                FROM @HetHan
                            ) AS h ON h.MaDatBan = w.MaDatBan
                            WHERE w.TrangThai IN (N'Đang chờ duyệt', N'Đã xác nhận');

                            /*
                               4. Chỉ trả bàn về Trống khi:
                                  - Bàn đang ở trạng thái Đã đặt (không đụng tới Đang dùng/Bảo trì).
                                  - Không còn phiếu đặt WinForms đang chờ xác nhận, đã xác nhận
                                    hoặc khách đã đến.
                                  - Không còn đơn POS mới, đã xác nhận hoặc đang chế biến.
                                  - Không còn phiếu Website đang giữ chỗ/chờ duyệt/đã xác nhận/
                                    đang chế biến cho khu vực có tên tương ứng với bàn này.

                               Web_KhuVuc được đồng bộ tên từ Ban trong DatBanService,
                               nên phép nối theo tên giúp giữ bàn nếu có phiếu Web chưa kịp
                               tạo bản ghi DatBan tương ứng trong WinForms.
                            */
                            UPDATE b
                            SET TrangThai = N'Trống'
                            FROM dbo.Ban AS b WITH (UPDLOCK, HOLDLOCK)
                            INNER JOIN
                            (
                                SELECT DISTINCT BanId
                                FROM @HetHan
                                WHERE BanId IS NOT NULL
                            ) AS h ON h.BanId = b.Id
                            WHERE b.TrangThai = N'Đã đặt'
                              AND NOT EXISTS
                              (
                                  SELECT 1
                                  FROM dbo.DatBan AS d2 WITH (UPDLOCK, HOLDLOCK)
                                  WHERE d2.BanId = b.Id
                                    AND d2.TrangThai IN
                                    (
                                        N'Chờ xác nhận',
                                        N'Đã xác nhận',
                                        N'Đã đến'
                                    )
                              )
                              AND NOT EXISTS
                              (
                                  SELECT 1
                                  FROM dbo.DonHang AS o2 WITH (UPDLOCK, HOLDLOCK)
                                  WHERE o2.BanId = b.Id
                                    AND o2.TrangThai IN
                                    (
                                        N'Mới',
                                        N'Đã xác nhận',
                                        N'Đang chế biến'
                                    )
                              )
                              AND NOT EXISTS
                              (
                                  SELECT 1
                                  FROM dbo.Web_DatBan AS w2 WITH (UPDLOCK, HOLDLOCK)
                                  INNER JOIN dbo.Web_KhuVuc AS kv
                                      ON kv.Id = w2.KhuVucId
                                  WHERE UPPER(LTRIM(RTRIM(kv.Ten))) =
                                        UPPER(LTRIM(RTRIM(b.TenBan)))
                                    AND
                                    (
                                        w2.TrangThai IN
                                        (
                                            N'Đang chờ duyệt',
                                            N'Đã xác nhận',
                                            N'Đang chế biến',
                                            N'Đã đến'
                                        )
                                        OR
                                        (
                                            w2.TrangThai = N'Chờ đặt cọc'
                                            AND w2.GiuChoDen > @BayGio
                                        )
                                    )
                              );

                            COMMIT TRANSACTION;
                        END TRY
                        BEGIN CATCH
                            IF XACT_STATE() <> 0
                                ROLLBACK TRANSACTION;

                            THROW;
                        END CATCH;
                        """, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Không thể kiểm tra phiếu đặt bàn quá giờ hoặc giải phóng bàn. Lần kiểm tra tiếp theo sẽ thử lại.");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Ứng dụng đang tắt: dừng BackgroundService một cách bình thường.
        }
    }
}
