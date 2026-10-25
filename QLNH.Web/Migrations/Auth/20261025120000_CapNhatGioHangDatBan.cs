using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLNH.Web.Migrations.Auth
{
    /// <inheritdoc />
    public partial class CapNhatGioHangDatBan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ChoTiec",
                table: "Web_KhuVuc",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SoBan",
                table: "Web_DatBan",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AlterColumn<int>(
                name: "MucThucDonId",
                table: "Web_DatBanMon",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "MonAnId",
                table: "Web_DatBanMon",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MonAnId",
                table: "Web_DatBanMon");

            migrationBuilder.AlterColumn<int>(
                name: "MucThucDonId",
                table: "Web_DatBanMon",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "SoBan",
                table: "Web_DatBan");

            migrationBuilder.DropColumn(
                name: "ChoTiec",
                table: "Web_KhuVuc");
        }
    }
}
