USE [QLNhaHang];
GO

IF COL_LENGTH(N'dbo.DonHang', N'MaUuDai') IS NULL
BEGIN
    ALTER TABLE dbo.DonHang
    ADD MaUuDai nvarchar(30) NULL;
END;

IF COL_LENGTH(N'dbo.DonHang', N'GiamGia') IS NULL
BEGIN
    ALTER TABLE dbo.DonHang
    ADD GiamGia decimal(18, 0) NOT NULL
        CONSTRAINT DF_DonHang_GiamGia DEFAULT (0);
END;
GO

UPDATE o
SET o.MaUuDai = w.MaUuDai,
    o.GiamGia = w.GiamGia
FROM dbo.DonHang AS o
JOIN dbo.Web_DatBan AS w ON w.MaDatBan = o.MaDatBan
WHERE NULLIF(LTRIM(RTRIM(o.MaUuDai)), N'') IS NULL
  AND NULLIF(LTRIM(RTRIM(w.MaUuDai)), N'') IS NOT NULL;
GO

SELECT Id, MaDatBan, MaUuDai, GiamGia, TongTien
FROM dbo.DonHang
WHERE MaUuDai IS NOT NULL
ORDER BY Id DESC;
