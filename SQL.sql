USE [QLNhaHang];
GO

IF COL_LENGTH(N'dbo.DatBan', N'BatDauPhucVuLuc') IS NULL
BEGIN
    ALTER TABLE dbo.DatBan
    ADD BatDauPhucVuLuc datetime2 NULL;
END;
GO //Bổ sung thêm cột
