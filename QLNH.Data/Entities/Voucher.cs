
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
