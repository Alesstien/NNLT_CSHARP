# Hệ thống quản lý nhà hàng (C# - WinForms + ASP.NET Core MVC + SQL Server)

1. Chạy `Database/01_QLNhaHang.sql` trong SSMS.
2. Sửa `ConnectionStrings:Default` trong `QLNH.Web/appsettings.json` và `QLNH.Admin/appsettings.json`.
3. `powershell ./setup.ps1` (tạo solution, restore, build). Cần .NET 8 SDK + Visual Studio 2022 (workload .NET desktop + ASP.NET).
4. Chạy: `dotnet run --project QLNH.Web` và `dotnet run --project QLNH.Admin`.
5. Test: `dotnet test`.
Tài khoản: admin/admin123, nhanvien/nv123.
"# NNLT_CSHARP" 
