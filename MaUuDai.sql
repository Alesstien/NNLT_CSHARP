
CREATE TABLE dbo.Web_UuDai
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Ma NVARCHAR(50) NOT NULL,
    Ten NVARCHAR(150) NOT NULL,
    MoTa NVARCHAR(500) NULL,

    LoaiGiamGia NVARCHAR(20) NOT NULL,
    GiaTriGiam DECIMAL(18,2) NOT NULL,
    GiamToiDa DECIMAL(18,2) NULL,
    DonHangToiThieu DECIMAL(18,2) NOT NULL DEFAULT 0,

    NgayBatDau DATETIME2 NOT NULL,
    NgayKetThuc DATETIME2 NOT NULL,

    TrangThai BIT NOT NULL DEFAULT 1,

    CONSTRAINT UQ_Web_UuDai_Ma UNIQUE (Ma),

    CONSTRAINT CK_Web_UuDai_Loai
    CHECK (LoaiGiamGia IN (N'PhanTram', N'SoTien')),

    CONSTRAINT CK_Web_UuDai_GiaTri
    CHECK (GiaTriGiam > 0),

    CONSTRAINT CK_Web_UuDai_ThoiGian
    CHECK (NgayKetThuc > NgayBatDau)
);


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
