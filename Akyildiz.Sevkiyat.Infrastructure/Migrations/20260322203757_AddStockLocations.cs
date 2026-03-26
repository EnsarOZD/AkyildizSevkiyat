using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStockLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LocationTransfers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockMasterId = table.Column<int>(type: "int", nullable: false),
                    FromLocationId = table.Column<int>(type: "int", nullable: false),
                    ToLocationId = table.Column<int>(type: "int", nullable: false),
                    Qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TransferredByUserId = table.Column<int>(type: "int", nullable: true),
                    TransferredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationTransfers_StockMasters_StockMasterId",
                        column: x => x.StockMasterId,
                        principalTable: "StockMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LocationTransfers_WarehouseLocations_FromLocationId",
                        column: x => x.FromLocationId,
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LocationTransfers_WarehouseLocations_ToLocationId",
                        column: x => x.ToLocationId,
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockMasterId = table.Column<int>(type: "int", nullable: false),
                    WarehouseLocationId = table.Column<int>(type: "int", nullable: false),
                    OnHandQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    ReservedQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    LastMovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockLocations_StockMasters_StockMasterId",
                        column: x => x.StockMasterId,
                        principalTable: "StockMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockLocations_WarehouseLocations_WarehouseLocationId",
                        column: x => x.WarehouseLocationId,
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LocationTransfers_FromLocationId",
                table: "LocationTransfers",
                column: "FromLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationTransfers_StockMasterId",
                table: "LocationTransfers",
                column: "StockMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationTransfers_ToLocationId",
                table: "LocationTransfers",
                column: "ToLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationTransfers_TransferredAt",
                table: "LocationTransfers",
                column: "TransferredAt");

            migrationBuilder.CreateIndex(
                name: "IX_StockLocations_StockMasterId_WarehouseLocationId",
                table: "StockLocations",
                columns: new[] { "StockMasterId", "WarehouseLocationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockLocations_WarehouseLocationId",
                table: "StockLocations",
                column: "WarehouseLocationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocationTransfers");

            migrationBuilder.DropTable(
                name: "StockLocations");
        }
    }
}
