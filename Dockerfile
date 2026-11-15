# 1.biên dịch mã nguồn (build)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# tạo và thiết lập thư mục bên trong container là src
WORKDIR /src

# sao chép tệp cấu hình dự án (.csproj) vào container
COPY ["QLNH.Data/QLNH.Data.csproj", "QLNH.Data/"]
COPY ["QLNH.Web/QLNH.Web.csproj", "QLNH.Web/"]

# tải và khôi phục các gói thư viện NuGet packages cần thiết 
RUN dotnet restore "QLNH.Web/QLNH.Web.csproj"

# sao chép mã nguồn 
COPY QLNH.Data/ QLNH.Data/
COPY QLNH.Web/ QLNH.Web/

# chuyển thư mục vào trong web chính
WORKDIR "/src/QLNH.Web"

# biên dịch ở chế độ Release), xuất tệp chạy ra thư mục /app/publish
# /p:UseAppHost=false: không tạo tệp thực thi riêng 
RUN dotnet publish "QLNH.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. đóng gói môi trường chạy (final)

# dùng image chỉ chứa ASP.NET Core Runtime 8.0
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# thiết lập thư mục của ứng dụng khi chạy thực tế là /app 
WORKDIR /app

# lấy thành phầm đã biên dịch ở /app/publish từ build sang thư mục hiện tại 
COPY --from=build /app/publish .

# yêu cầu lắng nghe kết nối http 8080
ENV ASPNETCORE_HTTP_PORTS=8080

# khai báo docker biết container sẽ mở cổng 8080 để giao tiếp bên ngoài 
EXPOSE 8080

# tự động thực thi
ENTRYPOINT ["dotnet", "QLNH.Web.dll"]
