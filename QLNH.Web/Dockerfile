FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["QLNH.Data/QLNH.Data.csproj", "QLNH.Data/"]
COPY ["QLNH.Web/QLNH.Web.csproj", "QLNH.Web/"]
RUN dotnet restore "QLNH.Web/QLNH.Web.csproj"

COPY QLNH.Data/ QLNH.Data/
COPY QLNH.Web/ QLNH.Web/
WORKDIR "/src/QLNH.Web"
RUN dotnet publish "QLNH.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "QLNH.Web.dll"]
