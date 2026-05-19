using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WmsPhase1_BarcodeAndPutaway : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "Alan",
                table: "WarehouseLocations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QrCode",
                table: "WarehouseLocations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalFloors",
                table: "WarehouseLocations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WmsBarcodePickingEnabled",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "WmsLocationPickingEnabled",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "WmsPutawayEnabled",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "StockMasters",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultPickingFaceId",
                table: "StockMasters",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PutawayTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GoodsReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GoodsReceiptLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StockMasterId = table.Column<int>(type: "int", nullable: false),
                    TotalQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    DistributedQty = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PutawayTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PutawayTasks_GoodsReceiptLines_GoodsReceiptLineId",
                        column: x => x.GoodsReceiptLineId,
                        principalTable: "GoodsReceiptLines",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PutawayTasks_GoodsReceipts_GoodsReceiptId",
                        column: x => x.GoodsReceiptId,
                        principalTable: "GoodsReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PutawayTasks_StockMasters_StockMasterId",
                        column: x => x.StockMasterId,
                        principalTable: "StockMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockBarcodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockMasterId = table.Column<int>(type: "int", nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockBarcodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockBarcodes_StockMasters_StockMasterId",
                        column: x => x.StockMasterId,
                        principalTable: "StockMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PutawayLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PutawayTaskId = table.Column<int>(type: "int", nullable: false),
                    Qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    WarehouseLocationId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PutawayLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PutawayLines_PutawayTasks_PutawayTaskId",
                        column: x => x.PutawayTaskId,
                        principalTable: "PutawayTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PutawayLines_WarehouseLocations_WarehouseLocationId",
                        column: x => x.WarehouseLocationId,
                        principalTable: "WarehouseLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "IssOrders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeliveryDate", "OrderDate" },
                values: new object[] { new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Local) });

            migrationBuilder.UpdateData(
                table: "StockMasters",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Barcode", "DefaultPickingFaceId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "StockMasters",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Barcode", "DefaultPickingFaceId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "StockMasters",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Barcode", "DefaultPickingFaceId" },
                values: new object[] { null, null });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocations_QrCode",
                table: "WarehouseLocations",
                column: "QrCode");

            migrationBuilder.CreateIndex(
                name: "IX_StockMasters_Barcode",
                table: "StockMasters",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_StockMasters_DefaultPickingFaceId",
                table: "StockMasters",
                column: "DefaultPickingFaceId");

            migrationBuilder.CreateIndex(
                name: "IX_PutawayLines_PutawayTaskId",
                table: "PutawayLines",
                column: "PutawayTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_PutawayLines_WarehouseLocationId",
                table: "PutawayLines",
                column: "WarehouseLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_PutawayTasks_GoodsReceiptId_Status",
                table: "PutawayTasks",
                columns: new[] { "GoodsReceiptId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PutawayTasks_GoodsReceiptLineId",
                table: "PutawayTasks",
                column: "GoodsReceiptLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PutawayTasks_StockMasterId",
                table: "PutawayTasks",
                column: "StockMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_StockBarcodes_Barcode",
                table: "StockBarcodes",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_StockBarcodes_StockMasterId",
                table: "StockBarcodes",
                column: "StockMasterId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMasters_WarehouseLocations_DefaultPickingFaceId",
                table: "StockMasters",
                column: "DefaultPickingFaceId",
                principalTable: "WarehouseLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMasters_WarehouseLocations_DefaultPickingFaceId",
                table: "StockMasters");

            migrationBuilder.DropTable(
                name: "PutawayLines");

            migrationBuilder.DropTable(
                name: "StockBarcodes");

            migrationBuilder.DropTable(
                name: "PutawayTasks");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseLocations_QrCode",
                table: "WarehouseLocations");

            migrationBuilder.DropIndex(
                name: "IX_StockMasters_Barcode",
                table: "StockMasters");

            migrationBuilder.DropIndex(
                name: "IX_StockMasters_DefaultPickingFaceId",
                table: "StockMasters");

            migrationBuilder.DropColumn(
                name: "Alan",
                table: "WarehouseLocations");

            migrationBuilder.DropColumn(
                name: "QrCode",
                table: "WarehouseLocations");

            migrationBuilder.DropColumn(
                name: "TotalFloors",
                table: "WarehouseLocations");

            migrationBuilder.DropColumn(
                name: "WmsBarcodePickingEnabled",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "WmsLocationPickingEnabled",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "WmsPutawayEnabled",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "StockMasters");

            migrationBuilder.DropColumn(
                name: "DefaultPickingFaceId",
                table: "StockMasters");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "WarehouseLocations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.UpdateData(
                table: "IssOrders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeliveryDate", "OrderDate" },
                values: new object[] { new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 5, 14, 0, 0, 0, 0, DateTimeKind.Local) });
        }
    }
}
