using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverAndVehicleEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DriverName",
                table: "ZonePreparations");

            migrationBuilder.DropColumn(
                name: "PlateNumber",
                table: "ZonePreparations");

            migrationBuilder.AddColumn<int>(
                name: "DriverId",
                table: "ZonePreparations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VehicleId",
                table: "ZonePreparations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Drivers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drivers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlateNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Capacity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ZonePreparations_DriverId",
                table: "ZonePreparations",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_ZonePreparations_VehicleId",
                table: "ZonePreparations",
                column: "VehicleId");

            migrationBuilder.AddForeignKey(
                name: "FK_ZonePreparations_Drivers_DriverId",
                table: "ZonePreparations",
                column: "DriverId",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ZonePreparations_Vehicles_VehicleId",
                table: "ZonePreparations",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ZonePreparations_Drivers_DriverId",
                table: "ZonePreparations");

            migrationBuilder.DropForeignKey(
                name: "FK_ZonePreparations_Vehicles_VehicleId",
                table: "ZonePreparations");

            migrationBuilder.DropTable(
                name: "Drivers");

            migrationBuilder.DropTable(
                name: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_ZonePreparations_DriverId",
                table: "ZonePreparations");

            migrationBuilder.DropIndex(
                name: "IX_ZonePreparations_VehicleId",
                table: "ZonePreparations");

            migrationBuilder.DropColumn(
                name: "DriverId",
                table: "ZonePreparations");

            migrationBuilder.DropColumn(
                name: "VehicleId",
                table: "ZonePreparations");

            migrationBuilder.AddColumn<string>(
                name: "DriverName",
                table: "ZonePreparations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlateNumber",
                table: "ZonePreparations",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
