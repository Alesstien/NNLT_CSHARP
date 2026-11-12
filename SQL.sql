USE [QLNhaHang];
GO

-- Bổ sung thêm cột phục vụ đồng bộ đặt bàn và ưu đãi
IF COL_LENGTH(N'dbo.DatBan', N'BatDauPhucVuLuc') IS NULL
BEGIN
    ALTER TABLE dbo.DatBan
    ADD BatDauPhucVuLuc datetime2 NULL;
END;

IF COL_LENGTH(N'dbo.DonHang', N'MaDatBan') IS NULL
BEGIN
    ALTER TABLE dbo.DonHang
    ADD MaDatBan nvarchar(30) NULL;
END;

IF COL_LENGTH(N'dbo.DonHang', N'MaUuDai') IS NULL
BEGIN
    ALTER TABLE dbo.DonHang
    ADD MaUuDai nvarchar(30) NULL;
END;

IF COL_LENGTH(N'dbo.DonHang', N'GiamGia') IS NULL
BEGIN
    ALTER TABLE dbo.DonHang
    ADD GiamGia decimal(18,2) NOT NULL CONSTRAINT DF_DonHang_GiamGia DEFAULT(0);
END;
GO
