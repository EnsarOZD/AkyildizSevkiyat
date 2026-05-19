using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VehicleReturns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DriverSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleReturns_DriverSessions_DriverSessionId",
                        column: x => x.DriverSessionId,
                        principalTable: "DriverSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VehicleReturnLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleReturnId = table.Column<int>(type: "int", nullable: false),
                    StockMasterId = table.Column<int>(type: "int", nullable: true),
                    StockCodeFree = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StockNameFree = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Qty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LinkedShipmentId = table.Column<int>(type: "int", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleReturnLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleReturnLines_Shipments_LinkedShipmentId",
                        column: x => x.LinkedShipmentId,
                        principalTable: "Shipments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VehicleReturnLines_StockMasters_StockMasterId",
                        column: x => x.StockMasterId,
                        principalTable: "StockMasters",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VehicleReturnLines_VehicleReturns_VehicleReturnId",
                        column: x => x.VehicleReturnId,
                        principalTable: "VehicleReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleReturnLines_LinkedShipmentId",
                table: "VehicleReturnLines",
                column: "LinkedShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleReturnLines_StockMasterId",
                table: "VehicleReturnLines",
                column: "StockMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleReturnLines_VehicleReturnId",
                table: "VehicleReturnLines",
                column: "VehicleReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleReturns_DriverSessionId",
                table: "VehicleReturns",
                column: "DriverSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VehicleReturnLines");

            migrationBuilder.DropTable(
                name: "VehicleReturns");
        }
    }
}
