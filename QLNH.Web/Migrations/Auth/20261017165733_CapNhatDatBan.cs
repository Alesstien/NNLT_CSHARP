using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLNH.Web.Migrations.Auth
{
    /// <inheritdoc />
    public partial class CapNhatDatBan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LanDangNhapCuoi",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Web_KhuVuc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ten = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    PhuThu = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    SucChua = table.Column<int>(type: "int", nullable: false),
                    HoatDong = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Web_KhuVuc", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Web_MucThucDon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ten = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Gia = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    DanhMuc = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    HinhAnh = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Emoji = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ThuTu = table.Column<int>(type: "int", nullable: false),
                    ConPhucVu = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Web_MucThucDon", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Web_UuDai",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ma = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhanTramGiam = table.Column<int>(type: "int", nullable: false),
                    GiamToiDa = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: true),
                    HanDung = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HoatDong = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Web_UuDai", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Web_DatBan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDatBan = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SoDienThoai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NgayGio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SoKhach = table.Column<int>(type: "int", nullable: false),
                    KhuVucId = table.Column<int>(type: "int", nullable: false),
                    DipDacBiet = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TongMon = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    PhuThuKhuVuc = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    Vat = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    PhiPhucVu = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    MaUuDai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    GiamGia = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    TongHoaDon = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    CheDoCoc = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TienCoc = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    ConLai = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    PhuongThucThanhToan = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NganHang = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    MaGiaoDich = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DaThanhToanCoc = table.Column<bool>(type: "bit", nullable: false),
                    NgayThanhToan = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GiuChoDen = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LyDoHuy = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Web_DatBan", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Web_DatBan_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Web_DatBan_Web_KhuVuc_KhuVucId",
                        column: x => x.KhuVucId,
                        principalTable: "Web_KhuVuc",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Web_DatBanMon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DatBanId = table.Column<int>(type: "int", nullable: false),
                    MucThucDonId = table.Column<int>(type: "int", nullable: false),
                    TenMon = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DonGia = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Web_DatBanMon", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Web_DatBanMon_Web_DatBan_DatBanId",
                        column: x => x.DatBanId,
                        principalTable: "Web_DatBan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Web_DatBanMon_Web_MucThucDon_MucThucDonId",
                        column: x => x.MucThucDonId,
                        principalTable: "Web_MucThucDon",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Web_DatBan_KhuVucId_NgayGio",
                table: "Web_DatBan",
                columns: new[] { "KhuVucId", "NgayGio" });

            migrationBuilder.CreateIndex(
                name: "IX_Web_DatBan_MaDatBan",
                table: "Web_DatBan",
                column: "MaDatBan",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Web_DatBan_UserId",
                table: "Web_DatBan",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Web_DatBanMon_DatBanId",
                table: "Web_DatBanMon",
                column: "DatBanId");

            migrationBuilder.CreateIndex(
                name: "IX_Web_DatBanMon_MucThucDonId",
                table: "Web_DatBanMon",
                column: "MucThucDonId");

            migrationBuilder.CreateIndex(
                name: "IX_Web_UuDai_Ma",
                table: "Web_UuDai",
                column: "Ma",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Web_DatBanMon");

            migrationBuilder.DropTable(
                name: "Web_UuDai");

            migrationBuilder.DropTable(
                name: "Web_DatBan");

            migrationBuilder.DropTable(
                name: "Web_MucThucDon");

            migrationBuilder.DropTable(
                name: "Web_KhuVuc");

            migrationBuilder.DropColumn(
                name: "LanDangNhapCuoi",
                table: "AspNetUsers");
        }
    }
}
