using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StockManagement_Infrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "StockMasters",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Unit",
                table: "StockMasters",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TaxRate",
                table: "StockMasters",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Category",
                table: "StockMasters",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "StockMasters",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinStockQty",
                table: "StockMasters",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OnHandQty",
                table: "StockMasters",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReservedQty",
                table: "StockMasters",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "WarehouseLocation",
                table: "StockMasters",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ExternalStockCode",
                table: "StockMappings",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.Sql(@"
                UPDATE [ShipmentLines] SET [Unit] = CASE
                    WHEN [Unit] = 'Adet' THEN '0'
                    WHEN [Unit] = 'Kg' THEN '1'
                    WHEN [Unit] = 'Paket' THEN '2'
                    WHEN [Unit] = 'Koli' THEN '3'
                    WHEN [Unit] = 'Litre' THEN '4'
                    WHEN [Unit] = 'Metre' THEN '5'
                    WHEN [Unit] = 'Metrekare' THEN '6'
                    WHEN [Unit] = 'Set' THEN '7'
                    WHEN [Unit] = 'Teneke' THEN '8'
                    WHEN [Unit] = 'Diger' THEN '99'
                    ELSE '0' END");

            migrationBuilder.AlterColumn<int>(
                name: "Unit",
                table: "ShipmentLines",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "StockMasterId",
                table: "ShipmentLines",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE [PurchaseOrderLines] SET [Unit] = CASE
                    WHEN [Unit] = 'Adet' THEN '0'
                    WHEN [Unit] = 'Kg' THEN '1'
                    WHEN [Unit] = 'Paket' THEN '2'
                    WHEN [Unit] = 'Koli' THEN '3'
                    WHEN [Unit] = 'Litre' THEN '4'
                    WHEN [Unit] = 'Metre' THEN '5'
                    WHEN [Unit] = 'Metrekare' THEN '6'
                    WHEN [Unit] = 'Set' THEN '7'
                    WHEN [Unit] = 'Teneke' THEN '8'
                    WHEN [Unit] = 'Diger' THEN '99'
                    ELSE '0' END");

            migrationBuilder.AlterColumn<int>(
                name: "Unit",
                table: "PurchaseOrderLines",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "IssOrders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.Sql(@"
                UPDATE [IssOrderLines] SET [Unit] = CASE
                    WHEN [Unit] = 'Adet' THEN '0'
                    WHEN [Unit] = 'Kg' THEN '1'
                    WHEN [Unit] = 'Paket' THEN '2'
                    WHEN [Unit] = 'Koli' THEN '3'
                    WHEN [Unit] = 'Litre' THEN '4'
                    WHEN [Unit] = 'Metre' THEN '5'
                    WHEN [Unit] = 'Metrekare' THEN '6'
                    WHEN [Unit] = 'Set' THEN '7'
                    WHEN [Unit] = 'Teneke' THEN '8'
                    WHEN [Unit] = 'Diger' THEN '99'
                    ELSE '0' END");

            migrationBuilder.AlterColumn<int>(
                name: "Unit",
                table: "IssOrderLines",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.Sql(@"
                UPDATE [GoodsReceiptLines] SET [UnitSnapshot] = CASE
                    WHEN [UnitSnapshot] = 'Adet' THEN '0'
                    WHEN [UnitSnapshot] = 'Kg' THEN '1'
                    WHEN [UnitSnapshot] = 'Paket' THEN '2'
                    WHEN [UnitSnapshot] = 'Koli' THEN '3'
                    WHEN [UnitSnapshot] = 'Litre' THEN '4'
                    WHEN [UnitSnapshot] = 'Metre' THEN '5'
                    WHEN [UnitSnapshot] = 'Metrekare' THEN '6'
                    WHEN [UnitSnapshot] = 'Set' THEN '7'
                    WHEN [UnitSnapshot] = 'Teneke' THEN '8'
                    WHEN [UnitSnapshot] = 'Diger' THEN '99'
                    ELSE '0' END
                WHERE [UnitSnapshot] IS NOT NULL");

            migrationBuilder.AlterColumn<int>(
                name: "UnitSnapshot",
                table: "GoodsReceiptLines",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "StockTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockMasterId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Qty = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransactions_StockMasters_StockMasterId",
                        column: x => x.StockMasterId,
                        principalTable: "StockMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "IssOrderLines",
                keyColumn: "Id",
                keyValue: 1,
                column: "Unit",
                value: 0);

            migrationBuilder.UpdateData(
                table: "IssOrders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeliveryDate", "OrderDate" },
                values: new object[] { new DateTime(2026, 3, 19, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 3, 18, 0, 0, 0, 0, DateTimeKind.Local) });

            migrationBuilder.UpdateData(
                table: "StockMasters",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Brand", "Category", "MinStockQty", "TaxRate", "Unit", "UnitPrice", "WarehouseLocation" },
                values: new object[] { null, 0, null, 20, 0, 0m, null });

            migrationBuilder.UpdateData(
                table: "StockMasters",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Brand", "Category", "MinStockQty", "TaxRate", "Unit", "UnitPrice", "WarehouseLocation" },
                values: new object[] { null, 0, null, 20, 0, 0m, null });

            migrationBuilder.UpdateData(
                table: "StockMasters",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Brand", "Category", "MinStockQty", "TaxRate", "Unit", "UnitPrice", "WarehouseLocation" },
                values: new object[] { null, 0, null, 20, 0, 0m, null });

            migrationBuilder.CreateIndex(
                name: "IX_StockMappings_ExternalStockCode",
                table: "StockMappings",
                column: "ExternalStockCode");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_DeliveryDate",
                table: "Shipments",
                column: "DeliveryDate");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_Status",
                table: "Shipments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentLines_StockMasterId",
                table: "ShipmentLines",
                column: "StockMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentHistories_ShipmentId_ChangedAt",
                table: "ShipmentHistories",
                columns: new[] { "ShipmentId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_StockMasterId_Date",
                table: "StockTransactions",
                columns: new[] { "StockMasterId", "Date" });

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentHistories_Shipments_ShipmentId",
                table: "ShipmentHistories",
                column: "ShipmentId",
                principalTable: "Shipments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShipmentLines_StockMasters_StockMasterId",
                table: "ShipmentLines",
                column: "StockMasterId",
                principalTable: "StockMasters",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentHistories_Shipments_ShipmentId",
                table: "ShipmentHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_ShipmentLines_StockMasters_StockMasterId",
                table: "ShipmentLines");

            migrationBuilder.DropTable(
                name: "StockTransactions");

            migrationBuilder.DropIndex(
                name: "IX_StockMappings_ExternalStockCode",
                table: "StockMappings");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_DeliveryDate",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_Status",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_ShipmentLines_StockMasterId",
                table: "ShipmentLines");

            migrationBuilder.DropIndex(
                name: "IX_ShipmentHistories_ShipmentId_ChangedAt",
                table: "ShipmentHistories");

            migrationBuilder.DropColumn(
                name: "Brand",
                table: "StockMasters");

            migrationBuilder.DropColumn(
                name: "MinStockQty",
                table: "StockMasters");

            migrationBuilder.DropColumn(
                name: "OnHandQty",
                table: "StockMasters");

            migrationBuilder.DropColumn(
                name: "ReservedQty",
                table: "StockMasters");

            migrationBuilder.DropColumn(
                name: "WarehouseLocation",
                table: "StockMasters");

            migrationBuilder.DropColumn(
                name: "StockMasterId",
                table: "ShipmentLines");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "StockMasters",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "StockMasters",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<decimal>(
                name: "TaxRate",
                table: "StockMasters",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "StockMasters",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "ExternalStockCode",
                table: "StockMappings",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "ShipmentLines",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "PurchaseOrderLines",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "IssOrders",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "IssOrderLines",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "UnitSnapshot",
                table: "GoodsReceiptLines",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.UpdateData(
                table: "IssOrderLines",
                keyColumn: "Id",
                keyValue: 1,
                column: "Unit",
                value: "Adet");

            migrationBuilder.UpdateData(
                table: "IssOrders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeliveryDate", "OrderDate" },
                values: new object[] { new DateTime(2026, 1, 28, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 1, 27, 0, 0, 0, 0, DateTimeKind.Local) });

            migrationBuilder.UpdateData(
                table: "StockMasters",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Category", "TaxRate", "Unit", "UnitPrice" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "StockMasters",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Category", "TaxRate", "Unit", "UnitPrice" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "StockMasters",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Category", "TaxRate", "Unit", "UnitPrice" },
                values: new object[] { null, null, null, null });
        }
    }
}
