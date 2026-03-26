using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Faz1_VehicleReturn_FloatingReturn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReturnNote",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReturnedAt",
                table: "Shipments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReturnReason",
                table: "ShipmentLines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReturnedQty",
                table: "ShipmentLines",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FloatingReturns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReturnDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StockMasterId = table.Column<int>(type: "int", nullable: true),
                    StockCodeFree = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StockNameFree = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ReturnReason = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LinkedShipmentId = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FloatingReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FloatingReturns_Shipments_LinkedShipmentId",
                        column: x => x.LinkedShipmentId,
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FloatingReturns_StockMasters_StockMasterId",
                        column: x => x.StockMasterId,
                        principalTable: "StockMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FloatingReturns_LinkedShipmentId",
                table: "FloatingReturns",
                column: "LinkedShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_FloatingReturns_StockMasterId",
                table: "FloatingReturns",
                column: "StockMasterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FloatingReturns");

            migrationBuilder.DropColumn(
                name: "ReturnNote",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "ReturnedAt",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "ReturnReason",
                table: "ShipmentLines");

            migrationBuilder.DropColumn(
                name: "ReturnedQty",
                table: "ShipmentLines");
        }
    }
}
