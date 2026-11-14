# Hệ Thống Quản Lý Nhà Hàng (NNLT C#)

Hệ thống Quản lý Nhà hàng tích hợp đồng bộ giữa **Ứng dụng Quản trị Desktop (C# WinForms)** và **Website Đặt món & Đặt bàn trực tuyến (ASP.NET Core MVC + Web API)** trên cùng một cơ sở dữ liệu **SQL Server**, tích hợp **Docker** và **CI/CD Pipeline** tự động qua GitHub Actions.

- **Repository GitHub**: https://github.com/Alesstien/NNLT_CSHARP

---

## 1. Kiến trúc giải pháp (`QuanLyNhaHang.sln`)

| Dự án | Công nghệ | Chức năng chính |
| :--- | :--- | :--- |
| **`QLNH.Data`** | EF Core 8, Class Library | Tầng dữ liệu dùng chung (`AppDbContext`), các thực thể (`MonAn`, `DanhMuc`, `Ban`, `DonHang`, `ChiTietDonHang`, `DatBan`, `TaiKhoan`, `VaiTro`), `OrderService`, `AuthService`, mã hóa mật khẩu `PBKDF2-SHA256`. |
| **`QLNH.Admin`** | C# WinForms (.NET 8) | Phần mềm quản lý tại nhà hàng: Quản lý Đơn hàng, Bán hàng tại bàn (POS), Quản lý Đặt bàn, Món ăn, Danh mục, Bàn ăn, Báo cáo thống kê Doanh thu và Quản lý Tài khoản/Phân quyền. |
| **`QLNH.Web`** | ASP.NET Core 8 MVC & REST API | Website cho khách hàng: Xem thực đơn, lọc/tìm kiếm món ăn, Giỏ hàng & Đặt bàn trực tuyến (hỗ trợ đặt kèm món hoặc giữ bàn), tính cọc VietQR, Đăng ký/Đăng nhập (ASP.NET Core Identity) và REST API. |
| **`QLNH.Tests`** | xUnit, EF Core InMemory | Bộ kiểm thử tự động (Unit Tests) cho các nghiệp vụ cốt lõi. |

---

## 2. Triển khai nhanh bằng Docker (Khuyên dùng)

Nếu máy tính của bạn đã cài **Docker Desktop**, bạn không cần cài đặt SQL Server hay .NET SDK lên máy. Chỉ cần mở Terminal tại thư mục gốc và chạy:

```bash
docker compose up -d
```

- Hệ thống sẽ tự động khởi động **SQL Server 2022** và ứng dụng **Website QLNH.Web**.
- Mở trình duyệt truy cập: **http://localhost:5000**
- Dừng toàn bộ hệ thống: `docker compose down`

---

## 3. Tự động hóa CI/CD với GitHub Actions

Dự án đã được tích hợp quy trình **CI/CD** tự động (`.github/workflows/ci-cd.yml`):
- **Tự động Build & Test (CI)**: Mỗi khi có commit đẩy lên nhánh `main`, GitHub Actions tự động cài đặt môi trường .NET 8, khôi phục dependencies, biên dịch toàn bộ Solution và chạy toàn bộ bài kiểm thử tự động (`QLNH.Tests`).
- **Tự động kiểm tra Docker (CD Preparation)**: Tự động đóng gói và xác thực Docker Image cho `QLNH.Web` trên môi trường Linux Ubuntu Runner.

---

## 4. Chạy thủ công trên máy tính (Không dùng Docker)

### Yêu cầu:
1. **.NET 8.0 SDK** trở lên.
2. **Microsoft SQL Server** (Express / Developer / LocalDB) và **SQL Server Management Studio (SSMS)**.

### Bước 1: Khởi tạo Cơ sở dữ liệu (SQL Server)
1. Mở **SSMS**, kết nối vào SQL Server của máy (`Server=.` hoặc `localhost`).
2. Mở và chạy script **`Database/01_QLNhaHang.sql`** (nhấn **F5 / Execute**) để tạo database `QLNhaHang`, các bảng và dữ liệu mẫu ban đầu.

### Bước 2: Biên dịch và chạy ứng dụng
Mở **PowerShell** tại thư mục dự án:

```powershell
# Khôi phục thư viện và build
dotnet restore
dotnet build

# Chạy Website (Terminal 1)
dotnet run --project QLNH.Web/QLNH.Web.csproj --urls "http://localhost:5000"

# Chạy Quản trị WinForms (Terminal 2)
dotnet run --project QLNH.Admin/QLNH.Admin.csproj

# Chạy kiểm thử tự động
dotnet test
```

---

## 5. Tài khoản đăng nhập mặc định

### Phần mềm WinForms (`QLNH.Admin`)
| Vai trò | Tài khoản | Mật khẩu | Quyền hạn |
| :--- | :--- | :--- | :--- |
| **Quản trị viên (`Admin`)** | `admin` | `admin123` | Toàn quyền 8 tab: Đơn hàng, POS, Đặt bàn, Món ăn, Danh mục, Bàn, Doanh thu, Tài khoản. |
| **Nhân viên (`NhanVien`)** | `nhanvien` | `nv123` | Nghiệp vụ phục vụ: Đơn hàng, Bán tại bàn (POS), Đặt bàn. |

### Website (`QLNH.Web`)
| Vai trò | Email / SĐT | Mật khẩu | Ghi chú |
| :--- | :--- | :--- | :--- |
| **Quản trị viên (`Admin`)** | `admin@nhahang.local` | `Admin@123` | Truy cập trang quản trị đơn đặt bàn `/Admin/Reservations`. |
| **Khách hàng** | Tự đăng ký tại `/Account/Register` | Tối thiểu 6 ký tự | Đăng nhập bằng Email hoặc Số điện thoại. |

---

## 6. Mã ưu đãi mẫu khi Đặt bàn trên Web
- **`WELCOME10`**: Giảm 10% tổng hóa đơn (tối đa `500.000 đ`).
- **`AURAVIP`**: Giảm 15% tổng hóa đơn (tối đa `2.000.000 đ`).
