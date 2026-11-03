# 🍜 Hệ Thống Quản Lý Nhà Hàng Hương Việt (NNLT C#)

Hệ thống Quản lý Nhà hàng tích hợp đồng bộ giữa **Ứng dụng Quản trị Desktop (C# WinForms)** và **Website Đặt món & Đặt bàn trực tuyến (ASP.NET Core MVC + Web API)** trên cùng một cơ sở dữ liệu **SQL Server**.

- **Repository GitHub**: https://github.com/Alesstien/NNLT_CSHARP

---

## 1. Kiến trúc giải pháp (`QuanLyNhaHang.sln`)

| Dự án            | Công nghệ                     | Chức năng chính                                                                                                                                                                                                                              |
| :--------------- | :---------------------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **`QLNH.Data`**  | EF Core 8, Class Library      | Tầng dữ liệu dùng chung (`AppDbContext`), các thực thể (`MonAn`, `DanhMuc`, `Ban`, `DonHang`, `ChiTietDonHang`, `DatBan`, `TaiKhoan`, `VaiTro`), `OrderService`, `AuthService`, mã hóa mật khẩu `PBKDF2-SHA256`.                             |
| **`QLNH.Admin`** | C# WinForms (.NET 8)          | Phần mềm quản lý tại nhà hàng: Quản lý Đơn hàng, Bán hàng tại bàn (POS), Quản lý Đặt bàn, Món ăn, Danh mục, Bàn ăn, Báo cáo thống kê Doanh thu (biểu đồ + xuất CSV UTF-8 BOM) và Quản lý Tài khoản/Phân quyền.                               |
| **`QLNH.Web`**   | ASP.NET Core 8 MVC & REST API | Website cho khách hàng: Xem thực đơn, lọc/tìm kiếm món ăn, Giỏ hàng & Đặt bàn trực tuyến (hỗ trợ đặt bàn kèm món hoặc đặt giữ bàn trước), tính cọc VietQR/MoMo/VNPay, Đăng ký/Đăng nhập (ASP.NET Core Identity) và REST API (`/api/mon-an`). |
| **`QLNH.Tests`** | xUnit, EF Core InMemory       | Bộ kiểm thử tự động (Unit Tests) cho các nghiệp vụ cốt lõi.                                                                                                                                                                                  |

---

## 2. Yêu cầu cài đặt

1. **.NET 8.0 SDK** trở lên.
2. **Microsoft SQL Server** (Express / Developer / LocalDB) và **SQL Server Management Studio (SSMS)**.
3. **Visual Studio 2022** (có cài workload _.NET desktop development_ và _ASP.NET and web development_) hoặc VS Code.

---

## 3. Khởi tạo Cơ sở dữ liệu (SQL Server)

1. Mở **SQL Server Management Studio (SSMS)**, kết nối vào SQL Server của máy (`Server=.` hoặc `localhost`).
2. Mở và chạy script **`Database/01_QLNhaHang.sql`** (nhấn **F5 / Execute**) để tạo database `QLNhaHang`, các bảng và dữ liệu mẫu ban đầu.
3. Kiểm tra chuỗi kết nối `ConnectionStrings:Default` trong 2 tệp cấu hình (mặc định đã trỏ sẵn tới `Server=.;Database=QLNhaHang`):
   - `QLNH.Web/appsettings.json`
   - `QLNH.Admin/appsettings.json`

_(Khi khởi chạy `QLNH.Web`, hệ thống sẽ tự động chạy Migration cho các bảng Identity/Web và đồng bộ dữ liệu bàn ăn, thực đơn, tài khoản và đơn đặt bàn giữa Web và WinForms Admin)._

---

## 4. Mã lệnh Terminal khởi chạy dự án

Mở **PowerShell** hoặc **Command Prompt (CMD)** tại thư mục gốc chứa `QuanLyNhaHang.sln`:

### 🔹 Bước 1: Khôi phục thư viện và Biên dịch toàn bộ Solution

```powershell
dotnet restore
dotnet build
```

### 🔹 Bước 2: Chạy Website (`QLNH.Web`)

Mở **Terminal 1** và chạy lệnh:

```powershell
dotnet run --project QLNH.Web/QLNH.Web.csproj --urls "http://localhost:5000"
```

### 🔹 Bước 3: Chạy Phần mềm Quản trị WinForms (`QLNH.Admin`)

Mở **Terminal 2** (để chạy song song với Web) và chạy lệnh:

```powershell
dotnet run --project QLNH.Admin/QLNH.Admin.csproj
```

_(Nếu `QLNH.Web` đang chạy trước đó, có thể thêm cờ `--no-build` để mở nhanh cửa sổ Admin mà không cần biên dịch lại `QLNH.Data`):_

```powershell
dotnet run --no-build --project QLNH.Admin/QLNH.Admin.csproj
```

### 🔹 Bước 4: Chạy Kiểm thử tự động (`QLNH.Tests`)

```powershell
dotnet test
```

---

## 5. Đường dẫn (URL) trên Website

Sau khi chạy lệnh khởi động `QLNH.Web`, mở trình duyệt và truy cập các đường dẫn sau:

| Trang | Đường dẫn (URL) |
| **Trang chủ & Lọc danh mục món** | http://localhost:5000 |

---

## 6. Tài khoản đăng nhập mặc định

### 🖥️ Phần mềm WinForms (`QLNH.Admin`)

| Vai trò | Tài khoản | Mật khẩu | Quyền hạn |

| **Quản trị viên (`Admin`)** | `admin` | `admin123`| Toàn quyền 8 tab: Đơn hàng, POS, Đặt bàn, Món ăn, Danh mục, Bàn, Doanh thu, Tài khoản. |
| **Nhân viên (`NhanVien`)** | `nhanvien` | `nv123` | Nghiệp vụ phục vụ 3 tab: Đơn hàng, Bán tại bàn (POS), Đặt bàn. |

### 🌐 Website (`QLNH.Web`)

| Vai trò | Email / SĐT | Mật khẩu | Ghi chú |

## | **Khách hàng (`KhachHang`)** | Tự đăng ký tại `/Account/Register` | Tối thiểu 6 ký tự (có chữ thường + chữ số) | Hỗ trợ đăng nhập bằng cả **Email** hoặc **Số điện thoại**. |

## 7. Mã ưu đãi mẫu khi Đặt bàn trên Web

- **`WELCOME10`**: Giảm 10% tiền món (tối đa `500.000 đ`).
- **`AURAVIP`**: Giảm 15% tiền món (tối đa `2.000.000 đ`).
