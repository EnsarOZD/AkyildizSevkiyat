using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorWarehouseLocationToKoridorModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rack",
                table: "WarehouseLocations");

            migrationBuilder.DropColumn(
                name: "Row",
                table: "WarehouseLocations");

            migrationBuilder.DropColumn(
                name: "Slot",
                table: "WarehouseLocations");

            migrationBuilder.DropColumn(
                name: "Zone",
                table: "WarehouseLocations");

            migrationBuilder.RenameColumn(
                name: "Level",
                table: "WarehouseLocations",
                newName: "ModulNo");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "WarehouseLocations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<int>(
                name: "Kat",
                table: "WarehouseLocations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "KoridorNo",
                table: "WarehouseLocations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Taraf",
                table: "WarehouseLocations",
                type: "nvarchar(1)",
                maxLength: 1,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocations_KoridorNo_Taraf_ModulNo_Kat",
                table: "WarehouseLocations",
                columns: new[] { "KoridorNo", "Taraf", "ModulNo", "Kat" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WarehouseLocations_KoridorNo_Taraf_ModulNo_Kat",
                table: "WarehouseLocations");

            migrationBuilder.DropColumn(
                name: "Kat",
                table: "WarehouseLocations");

            migrationBuilder.DropColumn(
                name: "KoridorNo",
                table: "WarehouseLocations");

            migrationBuilder.DropColumn(
                name: "Taraf",
                table: "WarehouseLocations");

            migrationBuilder.RenameColumn(
                name: "ModulNo",
                table: "WarehouseLocations",
                newName: "Level");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "WarehouseLocations",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "Rack",
                table: "WarehouseLocations",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Row",
                table: "WarehouseLocations",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Slot",
                table: "WarehouseLocations",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Zone",
                table: "WarehouseLocations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }
    }
}
