using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleQrAndDriverSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QrCode",
                table: "Vehicles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "QrCodeGeneratedAt",
                table: "Vehicles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DriverSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DriverId = table.Column<int>(type: "int", nullable: false),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    ZonePreparationId = table.Column<int>(type: "int", nullable: true),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartLatitude = table.Column<double>(type: "float", nullable: false),
                    StartLongitude = table.Column<double>(type: "float", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndLatitude = table.Column<double>(type: "float", nullable: true),
                    EndLongitude = table.Column<double>(type: "float", nullable: true),
                    TotalDurationMinutes = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DeviceFingerprint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ClosedByUserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverSessions_Drivers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Drivers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DriverSessions_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DriverSessions_ZonePreparations_ZonePreparationId",
                        column: x => x.ZonePreparationId,
                        principalTable: "ZonePreparations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.UpdateData(
                table: "IssOrders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeliveryDate", "OrderDate" },
                values: new object[] { new DateTime(2026, 4, 12, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 4, 11, 0, 0, 0, 0, DateTimeKind.Local) });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_QrCode",
                table: "Vehicles",
                column: "QrCode",
                unique: true,
                filter: "[QrCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DriverSessions_DriverId_Status",
                table: "DriverSessions",
                columns: new[] { "DriverId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DriverSessions_StartTime",
                table: "DriverSessions",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_DriverSessions_VehicleId_Status",
                table: "DriverSessions",
                columns: new[] { "VehicleId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DriverSessions_ZonePreparationId",
                table: "DriverSessions",
                column: "ZonePreparationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriverSessions");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_QrCode",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "QrCode",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "QrCodeGeneratedAt",
                table: "Vehicles");

            migrationBuilder.UpdateData(
                table: "IssOrders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeliveryDate", "OrderDate" },
                values: new object[] { new DateTime(2026, 4, 10, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 4, 9, 0, 0, 0, 0, DateTimeKind.Local) });
        }
    }
}
