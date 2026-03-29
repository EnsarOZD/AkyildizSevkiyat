using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleTypeAndZonePreparationDrivers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Vehicles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VehicleType",
                table: "Vehicles",
                type: "int",
                nullable: false,
                defaultValueSql: "0");

            migrationBuilder.CreateTable(
                name: "ZonePreparationDrivers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ZonePreparationId = table.Column<int>(type: "int", nullable: false),
                    DriverId = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZonePreparationDrivers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ZonePreparationDrivers_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ZonePreparationDrivers_ZonePreparations_ZonePreparationId",
                        column: x => x.ZonePreparationId,
                        principalTable: "ZonePreparations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "IssOrders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeliveryDate", "OrderDate" },
                values: new object[] { new DateTime(2026, 3, 30, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Local) });

            // Seed: mevcut ZonePreparation kayıtlarında DriverId IS NOT NULL olanlar
            migrationBuilder.Sql(@"
                INSERT INTO ZonePreparationDrivers (ZonePreparationId, DriverId, IsPrimary)
                SELECT Id, DriverId, 1
                FROM ZonePreparations
                WHERE DriverId IS NOT NULL
            ");

            migrationBuilder.CreateIndex(
                name: "IX_ZonePreparationDrivers_DriverId",
                table: "ZonePreparationDrivers",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_ZonePreparationDrivers_ZonePreparationId_DriverId",
                table: "ZonePreparationDrivers",
                columns: new[] { "ZonePreparationId", "DriverId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ZonePreparationDrivers");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "VehicleType",
                table: "Vehicles");

            migrationBuilder.UpdateData(
                table: "IssOrders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeliveryDate", "OrderDate" },
                values: new object[] { new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 3, 28, 0, 0, 0, 0, DateTimeKind.Local) });
        }
    }
}
