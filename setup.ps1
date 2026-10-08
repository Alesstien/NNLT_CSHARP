# Chạy một lần trong thư mục gốc để tạo Solution và khôi phục package
dotnet new sln -n QuanLyNhaHang
dotnet sln add QLNH.Data QLNH.Web QLNH.Admin QLNH.Tests
dotnet restore
dotnet build
