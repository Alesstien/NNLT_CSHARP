-- Create Database
CREATE DATABASE IF NOT EXISTS `qlnhahang` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `qlnhahang`;

-- 1. Tao cau truc cac bang
CREATE TABLE IF NOT EXISTS `vaitro` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `TenVaiTro` VARCHAR(50) NOT NULL UNIQUE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `taikhoan` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `TenDangNhap` VARCHAR(50) NOT NULL UNIQUE,
    `MatKhauHash` VARCHAR(200) NOT NULL,
    `HoTen` VARCHAR(100) NOT NULL,
    `VaiTroId` INT NOT NULL,
    `HoatDong` TINYINT(1) NOT NULL DEFAULT 1,
    FOREIGN KEY (`VaiTroId`) REFERENCES `vaitro`(`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `danhmuc` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `TenDanhMuc` VARCHAR(100) NOT NULL UNIQUE,
    `MoTa` VARCHAR(300) NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `monan` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `TenMon` VARCHAR(150) NOT NULL,
    `MoTa` VARCHAR(500) NULL,
    `Gia` DECIMAL(18,0) NOT NULL CHECK (`Gia` >= 0),
    `HinhAnh` VARCHAR(300) NULL,
    `DanhMucId` INT NOT NULL,
    `ConPhucVu` TINYINT(1) NOT NULL DEFAULT 1,
    FOREIGN KEY (`DanhMucId`) REFERENCES `danhmuc`(`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `ban` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `TenBan` VARCHAR(50) NOT NULL UNIQUE,
    `SoGhe` INT NOT NULL CHECK (`SoGhe` > 0),
    `TrangThai` VARCHAR(30) NOT NULL DEFAULT 'Trống'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `donhang` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `NgayTao` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `TenKhach` VARCHAR(100) NOT NULL,
    `SoDienThoai` VARCHAR(20) NULL,
    `BanId` INT NULL,
    `TrangThai` VARCHAR(30) NOT NULL DEFAULT 'Mới',
    `TongTien` DECIMAL(18,0) NOT NULL DEFAULT 0,
    `GhiChu` VARCHAR(300) NULL,
    FOREIGN KEY (`BanId`) REFERENCES `ban`(`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `chitietdonhang` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `DonHangId` INT NOT NULL,
    `MonAnId` INT NOT NULL,
    `SoLuong` INT NOT NULL CHECK (`SoLuong` > 0),
    `DonGia` DECIMAL(18,0) NOT NULL CHECK (`DonGia` >= 0),
    FOREIGN KEY (`DonHangId`) REFERENCES `donhang`(`Id`) ON DELETE CASCADE,
    FOREIGN KEY (`MonAnId`) REFERENCES `monan`(`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `datban` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `TenKhach` VARCHAR(100) NOT NULL,
    `SoDienThoai` VARCHAR(20) NOT NULL,
    `NgayGio` DATETIME NOT NULL,
    `SoNguoi` INT NOT NULL CHECK (`SoNguoi` > 0),
    `BanId` INT NULL,
    `TrangThai` VARCHAR(30) NOT NULL DEFAULT 'Chờ xác nhận',
    `GhiChu` VARCHAR(300) NULL,
    FOREIGN KEY (`BanId`) REFERENCES `ban`(`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

/* ===== DU LIEU MAU ===== */
SET FOREIGN_KEY_CHECKS = 0;

-- Xoa du lieu theo dung thu tu khoa ngoai
DELETE FROM `chitietdonhang`;
DELETE FROM `donhang`;
DELETE FROM `datban`;
DELETE FROM `monan`;
DELETE FROM `danhmuc`;
DELETE FROM `ban`;
DELETE FROM `taikhoan`;
DELETE FROM `vaitro`;

-- Reset ID tu tang ve 1
ALTER TABLE `chitietdonhang` AUTO_INCREMENT = 1;
ALTER TABLE `donhang` AUTO_INCREMENT = 1;
ALTER TABLE `datban` AUTO_INCREMENT = 1;
ALTER TABLE `monan` AUTO_INCREMENT = 1;
ALTER TABLE `danhmuc` AUTO_INCREMENT = 1;
ALTER TABLE `ban` AUTO_INCREMENT = 1;
ALTER TABLE `taikhoan` AUTO_INCREMENT = 1;
ALTER TABLE `vaitro` AUTO_INCREMENT = 1;

SET FOREIGN_KEY_CHECKS = 1;

-- 1. Vai tro
INSERT INTO `vaitro`(`TenVaiTro`) VALUES 
('Admin'),
('NhanVien');

-- 2. Tai khoan
INSERT INTO `taikhoan`(`TenDangNhap`, `MatKhauHash`, `HoTen`, `VaiTroId`) VALUES
('admin', 'AQIDBAUGBwgJCgsMDQ4PEA==$RmblliwioiHcibr884EC00ulmd3CUmcUOwSaMg2nlDM=', 'Quản trị viên', 1),
('nhanvien', 'ERITFBUWFxgZGhscHR4fIA==$/GZLFOy39uWPOR5NqVPKmecLLkVqlRnfWT6R4MmUC5w=', 'Nhân viên phục vụ', 2);

-- 3. Danh muc
INSERT INTO `danhmuc`(`TenDanhMuc`, `MoTa`) VALUES
('Khai vị', 'Món nhẹ dùng trước bữa chính'),
('Món chính', 'Các món ăn chính'),
('Lẩu & Nướng', 'Lẩu và đồ nướng'),
('Tráng miệng', 'Món ngọt kết thúc bữa ăn'),
('Đồ uống', 'Nước giải khát');

-- 4. Mon an
INSERT INTO `monan`(`TenMon`, `MoTa`, `Gia`, `HinhAnh`, `DanhMucId`) VALUES
('Gỏi cuốn tôm thịt', 'Cuốn tươi với tôm, thịt, bún, rau thơm', 45000, '/images/goi-cuon.jpg', 1),
('Chả giò hải sản', 'Chiên giòn, nhân hải sản', 55000, '/images/cha-gio.jpg', 1),
('Cơm tấm sườn bì chả', 'Sườn nướng, bì, chả trứng', 65000, '/images/com-tam.jpg', 2),
('Bò lúc lắc', 'Thịt bò Úc xào tỏi, khoai chiên', 129000, '/images/bo-luc-lac.jpg', 2),
('Cá lóc nướng trui', 'Cá lóc nướng rơm, ăn kèm rau sống', 189000, '/images/ca-loc-nuong.jpg', 2),
('Lẩu thái hải sản', 'Lẩu chua cay, hải sản tươi (2-3 người)', 329000, '/images/lau-thai.jpg', 3),
('Ba chỉ bò nướng', 'Ba chỉ bò Mỹ ướp sốt đặc biệt', 159000, '/images/ba-chi-bo.jpg', 3),
('Chè khúc bạch', 'Khúc bạch phô mai, hạnh nhân', 35000, '/images/che-khuc-bach.jpg', 4),
('Trà đào cam sả', 'Trà đào, cam tươi, sả', 39000, '/images/tra-dao.jpg', 5),
('Nước ép cam', 'Cam vắt nguyên chất', 42000, '/images/nuoc-cam.jpg', 5);

-- 5. Ban an
INSERT INTO `ban`(`TenBan`, `SoGhe`) VALUES 
('Bàn 01', 2),
('Bàn 02', 4),
('Bàn 03', 4),
('Bàn 04', 6),
('Bàn 05', 8),
('VIP 01', 10);