/* =============================================================
   HỆ THỐNG QUẢN LÝ NHÀ HÀNG
   Cơ sở dữ liệu: SQL Server (Transact-SQL)
   File: 01_QLNhaHang.sql
   Mô tả: Tạo cơ sở dữ liệu QLNhaHang, cấu trúc các bảng và dữ liệu mẫu khởi tạo.
   ============================================================= */

-- 1. TẠO CƠ SỞ DỮ LIỆU
IF DB_ID(N'QLNhaHang') IS NULL
BEGIN
    CREATE DATABASE QLNhaHang;
END
GO

USE QLNhaHang;
GO

-- 2. TẠO BẢNG CƠ SỞ DỮ LIỆU (NẾU CHƯA TỒN TẠI)

-- 2.1 Bảng Vai trò người dùng (Admin, Nhân viên)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'VaiTro')
BEGIN
    CREATE TABLE VaiTro (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        TenVaiTro NVARCHAR(50) NOT NULL UNIQUE
    );
END
GO

-- 2.2 Bảng Tài khoản người dùng
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TaiKhoan')
BEGIN
    CREATE TABLE TaiKhoan (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        TenDangNhap NVARCHAR(50) NOT NULL UNIQUE,
        MatKhauHash NVARCHAR(200) NOT NULL,
        HoTen NVARCHAR(100) NOT NULL,
        VaiTroId INT NOT NULL,
        HoatDong BIT NOT NULL DEFAULT 1,
        CONSTRAINT FK_TaiKhoan_VaiTro FOREIGN KEY (VaiTroId) REFERENCES VaiTro(Id)
    );
END
GO

-- 2.3 Bảng Danh mục món ăn
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DanhMuc')
BEGIN
    CREATE TABLE DanhMuc (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        TenDanhMuc NVARCHAR(100) NOT NULL UNIQUE,
        MoTa NVARCHAR(300) NULL
    );
END
GO

-- 2.4 Bảng Món ăn
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MonAn')
BEGIN
    CREATE TABLE MonAn (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        TenMon NVARCHAR(150) NOT NULL,
        MoTa NVARCHAR(500) NULL,
        Gia DECIMAL(18,0) NOT NULL CHECK (Gia >= 0),
        HinhAnh NVARCHAR(300) NULL,
        DanhMucId INT NOT NULL,
        ConPhucVu BIT NOT NULL DEFAULT 1,
        CONSTRAINT FK_MonAn_DanhMuc FOREIGN KEY (DanhMucId) REFERENCES DanhMuc(Id)
    );
END
GO

-- 2.5 Bảng Bàn ăn
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Ban')
BEGIN
    CREATE TABLE Ban (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        TenBan NVARCHAR(50) NOT NULL UNIQUE,
        SoGhe INT NOT NULL CHECK (SoGhe > 0),
        TrangThai NVARCHAR(30) NOT NULL DEFAULT N'Trống'
    );
END
GO

-- 2.6 Bảng Đơn hàng (Hóa đơn)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DonHang')
BEGIN
    CREATE TABLE DonHang (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        NgayTao DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        TenKhach NVARCHAR(100) NOT NULL,
        SoDienThoai NVARCHAR(20) NULL,
        BanId INT NULL,
        TrangThai NVARCHAR(30) NOT NULL DEFAULT N'Mới',
        TongTien DECIMAL(18,0) NOT NULL DEFAULT 0,
        GhiChu NVARCHAR(300) NULL,
        CONSTRAINT FK_DonHang_Ban FOREIGN KEY (BanId) REFERENCES Ban(Id) ON DELETE SET NULL
    );
END
GO

-- 2.7 Bảng Chi tiết đơn hàng
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChiTietDonHang')
BEGIN
    CREATE TABLE ChiTietDonHang (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        DonHangId INT NOT NULL,
        MonAnId INT NOT NULL,
        SoLuong INT NOT NULL CHECK (SoLuong > 0),
        DonGia DECIMAL(18,0) NOT NULL CHECK (DonGia >= 0),
        CONSTRAINT FK_ChiTietDonHang_DonHang FOREIGN KEY (DonHangId) REFERENCES DonHang(Id) ON DELETE CASCADE,
        CONSTRAINT FK_ChiTietDonHang_MonAn FOREIGN KEY (MonAnId) REFERENCES MonAn(Id)
    );
END
GO

-- 2.8 Bảng Đặt bàn
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DatBan')
BEGIN
    CREATE TABLE DatBan (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        TenKhach NVARCHAR(100) NOT NULL,
        SoDienThoai NVARCHAR(20) NOT NULL,
        NgayGio DATETIME2 NOT NULL,
        SoNguoi INT NOT NULL CHECK (SoNguoi > 0),
        BanId INT NULL,
        TrangThai NVARCHAR(30) NOT NULL DEFAULT N'Chờ xác nhận',
        GhiChu NVARCHAR(300) NULL,
        CONSTRAINT FK_DatBan_Ban FOREIGN KEY (BanId) REFERENCES Ban(Id) ON DELETE SET NULL
    );
END
GO

-- 3. NẠP DỮ LIỆU MẪU KHỞI TẠO (SEED DATA)
USE QLNhaHang;
GO

-- Xóa dữ liệu cũ nếu muốn thiết lập lại từ đầu
DELETE FROM ChiTietDonHang;
DELETE FROM DonHang;
DELETE FROM DatBan;
DELETE FROM MonAn;
DELETE FROM DanhMuc;
DELETE FROM Ban;
DELETE FROM TaiKhoan;
DELETE FROM VaiTro;

-- 3.1 Dữ liệu Vai trò
SET IDENTITY_INSERT VaiTro ON;
INSERT INTO VaiTro (Id, TenVaiTro) VALUES 
(1, N'Admin'),
(2, N'NhanVien');
SET IDENTITY_INSERT VaiTro OFF;

-- 3.2 Dữ liệu Tài khoản (Mật khẩu băm PBKDF2: admin -> admin123, nhanvien -> nv123)
SET IDENTITY_INSERT TaiKhoan ON;
INSERT INTO TaiKhoan (Id, TenDangNhap, MatKhauHash, HoTen, VaiTroId, HoatDong) VALUES
(1, N'admin', N'AQIDBAUGBwgJCgsMDQ4PEA==$RmblliwioiHcibr884EC00ulmd3CUmcUOwSaMg2nlDM=', N'Quản trị viên', 1, 1),
(2, N'nhanvien', N'ERITFBUWFxgZGhscHR4fIA==$/GZLFOy39uWPOR5NqVPKmecLLkVqlRnfWT6R4MmUC5w=', N'Nhân viên phục vụ', 2, 1);
SET IDENTITY_INSERT TaiKhoan OFF;

-- 3.3 Dữ liệu Danh mục món ăn
SET IDENTITY_INSERT DanhMuc ON;
INSERT INTO DanhMuc (Id, TenDanhMuc, MoTa) VALUES
(1, N'Khai vị', N'Món nhẹ dùng trước bữa chính'),
(2, N'Món chính', N'Các món ăn chính'),
(3, N'Lẩu & Nướng', N'Lẩu và đồ nướng'),
(4, N'Tráng miệng', N'Món ngọt kết thúc bữa ăn'),
(5, N'Đồ uống', N'Nước giải khát');
SET IDENTITY_INSERT DanhMuc OFF;

-- 3.4 Dữ liệu Món ăn
SET IDENTITY_INSERT MonAn ON;
INSERT INTO MonAn (Id, TenMon, MoTa, Gia, HinhAnh, DanhMucId, ConPhucVu) VALUES
(1, N'Gỏi cuốn tôm thịt', N'Cuốn tươi với tôm, thịt, bún, rau thơm', 45000, N'/images/goi-cuon.jpg', 1, 1),
(2, N'Chả giò hải sản', N'Chiên giòn, nhân hải sản', 55000, N'/images/cha-gio.jpg', 1, 1),
(3, N'Cơm tấm sườn bì chả', N'Sườn nướng, bì, chả trứng', 65000, N'/images/com-tam.jpg', 2, 1),
(4, N'Bò lúc lắc', N'Thịt bò Úc xào tỏi, khoai chiên', 129000, N'/images/bo-luc-lac.jpg', 2, 1),
(5, N'Cá lóc nướng trui', N'Cá lóc nướng rơm, ăn kèm rau sống', 189000, N'/images/ca-loc-nuong.jpg', 2, 1),
(6, N'Lẩu thái hải sản', N'Lẩu chua cay, hải sản tươi (2-3 người)', 329000, N'/images/lau-thai.jpg', 3, 1),
(7, N'Ba chỉ bò nướng', N'Ba chỉ bò Mỹ ướp sốt đặc biệt', 159000, N'/images/ba-chi-bo.jpg', 3, 1),
(8, N'Chè khúc bạch', N'Khúc bạch phô mai, hạnh nhân', 35000, N'/images/che-khuc-bach.jpg', 4, 1),
(9, N'Trà đào cam sả', N'Trà đào, cam tươi, sả', 39000, N'/images/tra-dao.jpg', 5, 1),
(10, N'Nước ép cam', N'Cam vắt nguyên chất', 42000, N'/images/nuoc-cam.jpg', 5, 1);
SET IDENTITY_INSERT MonAn OFF;

-- 3.5 Dữ liệu Bàn ăn
SET IDENTITY_INSERT Ban ON;
INSERT INTO Ban (Id, TenBan, SoGhe, TrangThai) VALUES 
(1, N'Bàn 01', 2, N'Trống'),
(2, N'Bàn 02', 4, N'Trống'),
(3, N'Bàn 03', 4, N'Trống'),
(4, N'Bàn 04', 6, N'Trống'),
(5, N'Bàn 05', 8, N'Trống'),
(6, N'VIP 01', 10, N'Trống');
SET IDENTITY_INSERT Ban OFF;

-- 3.6 Dữ liệu Đơn hàng mẫu (đã thanh toán trong 7 ngày gần nhất để phục vụ thống kê doanh thu)
SET IDENTITY_INSERT DonHang ON;
INSERT INTO DonHang (Id, NgayTao, TenKhach, SoDienThoai, BanId, TrangThai, TongTien, GhiChu) VALUES
(1, DATEADD(DAY, -5, SYSDATETIME()), N'Nguyễn Văn An', N'0901234567', 1, N'Đã thanh toán', 249000, N'Khách ăn trưa tại bàn'),
(2, DATEADD(DAY, -4, SYSDATETIME()), N'Trần Thị Bình', N'0912345678', 2, N'Đã thanh toán', 532000, N'Gia đình dùng bữa tối'),
(3, DATEADD(DAY, -3, SYSDATETIME()), N'Lê Hoàng Cường', N'0987654321', 4, N'Đã thanh toán', 677000, N'Liên hoan nhóm bạn'),
(4, DATEADD(DAY, -2, SYSDATETIME()), N'Phạm Minh Đức', N'0933445566', 3, N'Đã thanh toán', 453000, N'Khách tại bàn (Bàn 03)'),
(5, DATEADD(DAY, -1, SYSDATETIME()), N'Võ Thị Hoa', N'0977889900', 6, N'Đã thanh toán', 905000, N'Tiếp khách phòng VIP'),
(6, SYSDATETIME(), N'Đặng Quốc Khánh', N'0966554433', 2, N'Đã thanh toán', 383000, N'Khách ăn tại quán');
SET IDENTITY_INSERT DonHang OFF;

-- 3.7 Dữ liệu Chi tiết đơn hàng mẫu
SET IDENTITY_INSERT ChiTietDonHang ON;
INSERT INTO ChiTietDonHang (Id, DonHangId, MonAnId, SoLuong, DonGia) VALUES
(1, 1, 1, 2, 45000),
(2, 1, 3, 2, 65000),
(3, 1, 9, 1, 39000),
(4, 2, 2, 2, 55000),
(5, 2, 6, 1, 329000),
(6, 2, 9, 1, 39000),
(7, 2, 4, 1, 54000),
(8, 3, 5, 1, 189000),
(9, 3, 6, 1, 329000),
(10, 3, 7, 1, 159000),
(11, 4, 4, 2, 129000),
(12, 4, 7, 1, 159000),
(13, 4, 8, 1, 36000),
(14, 5, 6, 1, 329000),
(15, 5, 7, 2, 159000),
(16, 5, 4, 2, 129000),
(17, 6, 1, 2, 45000),
(18, 6, 5, 1, 189000),
(19, 6, 3, 1, 65000),
(20, 6, 9, 1, 39000);
SET IDENTITY_INSERT ChiTietDonHang OFF;

-- 3.8 Cập nhật giá trị IDENTITY tiếp theo cho các bảng
DBCC CHECKIDENT ('VaiTro', RESEED, 2);
DBCC CHECKIDENT ('TaiKhoan', RESEED, 2);
DBCC CHECKIDENT ('DanhMuc', RESEED, 5);
DBCC CHECKIDENT ('MonAn', RESEED, 10);
DBCC CHECKIDENT ('Ban', RESEED, 6);
DBCC CHECKIDENT ('DonHang', RESEED, 6);
DBCC CHECKIDENT ('ChiTietDonHang', RESEED, 20);
DBCC CHECKIDENT ('DatBan', RESEED, 0);
GO
