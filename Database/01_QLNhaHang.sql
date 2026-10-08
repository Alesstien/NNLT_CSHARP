/* ===== CSDL QUẢN LÝ NHÀ HÀNG - SQL Server (chuẩn 3NF) ===== */
IF DB_ID(N'QLNhaHang') IS NULL CREATE DATABASE QLNhaHang;
GO
USE QLNhaHang;
GO

-- 1. Tạo cấu trúc các bảng
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'VaiTro')
CREATE TABLE VaiTro(
    Id INT IDENTITY PRIMARY KEY,
    TenVaiTro NVARCHAR(50) NOT NULL UNIQUE
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TaiKhoan')
CREATE TABLE TaiKhoan(
    Id INT IDENTITY PRIMARY KEY,
    TenDangNhap NVARCHAR(50) NOT NULL UNIQUE,
    MatKhauHash NVARCHAR(200) NOT NULL,
    HoTen NVARCHAR(100) NOT NULL,
    VaiTroId INT NOT NULL REFERENCES VaiTro(Id),
    HoatDong BIT NOT NULL DEFAULT 1
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DanhMuc')
CREATE TABLE DanhMuc(
    Id INT IDENTITY PRIMARY KEY,
    TenDanhMuc NVARCHAR(100) NOT NULL UNIQUE,
    MoTa NVARCHAR(300) NULL
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MonAn')
CREATE TABLE MonAn(
    Id INT IDENTITY PRIMARY KEY,
    TenMon NVARCHAR(150) NOT NULL,
    MoTa NVARCHAR(500) NULL,
    Gia DECIMAL(18,0) NOT NULL CHECK (Gia >= 0),
    HinhAnh NVARCHAR(300) NULL,
    DanhMucId INT NOT NULL REFERENCES DanhMuc(Id),
    ConPhucVu BIT NOT NULL DEFAULT 1
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Ban')
CREATE TABLE Ban(
    Id INT IDENTITY PRIMARY KEY,
    TenBan NVARCHAR(50) NOT NULL UNIQUE,
    SoGhe INT NOT NULL CHECK (SoGhe > 0),
    TrangThai NVARCHAR(30) NOT NULL DEFAULT N'Trống'
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DonHang')
CREATE TABLE DonHang(
    Id INT IDENTITY PRIMARY KEY,
    NgayTao DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    TenKhach NVARCHAR(100) NOT NULL,
    SoDienThoai NVARCHAR(20) NULL,
    BanId INT NULL REFERENCES Ban(Id) ON DELETE SET NULL,
    TrangThai NVARCHAR(30) NOT NULL DEFAULT N'Mới',
    TongTien DECIMAL(18,0) NOT NULL DEFAULT 0,
    GhiChu NVARCHAR(300) NULL
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChiTietDonHang')
CREATE TABLE ChiTietDonHang(
    Id INT IDENTITY PRIMARY KEY,
    DonHangId INT NOT NULL REFERENCES DonHang(Id) ON DELETE CASCADE,
    MonAnId INT NOT NULL REFERENCES MonAn(Id),
    SoLuong INT NOT NULL CHECK (SoLuong > 0),
    DonGia DECIMAL(18,0) NOT NULL CHECK (DonGia >= 0)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DatBan')
CREATE TABLE DatBan(
    Id INT IDENTITY PRIMARY KEY,
    TenKhach NVARCHAR(100) NOT NULL,
    SoDienThoai NVARCHAR(20) NOT NULL,
    NgayGio DATETIME2 NOT NULL,
    SoNguoi INT NOT NULL CHECK (SoNguoi > 0),
    BanId INT NULL REFERENCES Ban(Id) ON DELETE SET NULL,
    TrangThai NVARCHAR(30) NOT NULL DEFAULT N'Chờ xác nhận',
    GhiChu NVARCHAR(300) NULL
);
GO

/* ===== DỮ LIỆU MẪU ===== */
USE QLNhaHang;
GO

-- Xóa dữ liệu cũ
DELETE FROM ChiTietDonHang;
DELETE FROM DonHang;
DELETE FROM DatBan;
DELETE FROM MonAn;
DELETE FROM DanhMuc;
DELETE FROM Ban;
DELETE FROM TaiKhoan;
DELETE FROM VaiTro;

-- Reset lại ID tự tăng về 0
DBCC CHECKIDENT ('ChiTietDonHang', RESEED, 0);
DBCC CHECKIDENT ('DonHang', RESEED, 0);
DBCC CHECKIDENT ('DatBan', RESEED, 0);
DBCC CHECKIDENT ('MonAn', RESEED, 0);
DBCC CHECKIDENT ('DanhMuc', RESEED, 0);
DBCC CHECKIDENT ('Ban', RESEED, 0);
DBCC CHECKIDENT ('TaiKhoan', RESEED, 0);
DBCC CHECKIDENT ('VaiTro', RESEED, 0);
GO

-- 1. Thêm Vai trò
INSERT INTO VaiTro(TenVaiTro) VALUES 
(N'Admin'),
(N'NhanVien');

-- 2. Thêm Tài khoản đăng nhập (mật khẩu tương ứng: admin123 / nv123)
INSERT INTO TaiKhoan(TenDangNhap, MatKhauHash, HoTen, VaiTroId) VALUES
(N'admin', N'AQIDBAUGBwgJCgsMDQ4PEA==$RmblliwioiHcibr884EC00ulmd3CUmcUOwSaMg2nlDM=', N'Quản trị viên', 1),
(N'nhanvien', N'ERITFBUWFxgZGhscHR4fIA==$/GZLFOy39uWPOR5NqVPKmecLLkVqlRnfWT6R4MmUC5w=', N'Nhân viên phục vụ', 2);

-- 3. Thêm Danh mục món ăn
INSERT INTO DanhMuc(TenDanhMuc, MoTa) VALUES
(N'Khai vị', N'Món nhẹ dùng trước bữa chính'),
(N'Món chính', N'Các món ăn chính'),
(N'Lẩu & Nướng', N'Lẩu và đồ nướng'),
(N'Tráng miệng', N'Món ngọt kết thúc bữa ăn'),
(N'Đồ uống', N'Nước giải khát');

-- 4. Thêm Món ăn (kèm đường dẫn hình ảnh)
INSERT INTO MonAn(TenMon, MoTa, Gia, HinhAnh, DanhMucId) VALUES
(N'Gỏi cuốn tôm thịt', N'Cuốn tươi với tôm, thịt, bún, rau thơm', 45000, N'/images/goi-cuon.jpg', 1),
(N'Chả giò hải sản', N'Chiên giòn, nhân hải sản', 55000, N'/images/cha-gio.jpg', 1),
(N'Cơm tấm sườn bì chả', N'Sườn nướng, bì, chả trứng', 65000, N'/images/com-tam.jpg', 2),
(N'Bò lúc lắc', N'Thịt bò Úc xào tỏi, khoai chiên', 129000, N'/images/bo-luc-lac.jpg', 2),
(N'Cá lóc nướng trui', N'Cá lóc nướng rơm, ăn kèm rau sống', 189000, N'/images/ca-loc-nuong.jpg', 2),
(N'Lẩu thái hải sản', N'Lẩu chua cay, hải sản tươi (2-3 người)', 329000, N'/images/lau-thai.jpg', 3),
(N'Ba chỉ bò nướng', N'Ba chỉ bò Mỹ ướp sốt đặc biệt', 159000, N'/images/ba-chi-bo.jpg', 3),
(N'Chè khúc bạch', N'Khúc bạch phô mai, hạnh nhân', 35000, N'/images/che-khuc-bach.jpg', 4),
(N'Trà đào cam sả', N'Trà đào, cam tươi, sả', 39000, N'/images/tra-dao.jpg', 5),
(N'Nước ép cam', N'Cam vắt nguyên chất', 42000, N'/images/nuoc-cam.jpg', 5);

-- 5. Thêm Danh sách Bàn ăn
INSERT INTO Ban(TenBan, SoGhe) VALUES 
(N'Bàn 01', 2),
(N'Bàn 02', 4),
(N'Bàn 03', 4),
(N'Bàn 04', 6),
(N'Bàn 05', 8),
(N'VIP 01', 10);
GO