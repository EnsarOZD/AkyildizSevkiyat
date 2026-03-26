using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReorderPointToStockMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ReorderPoint",
                table: "StockMasters",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "IssOrders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeliveryDate", "OrderDate" },
                values: new object[] { new DateTime(2026, 3, 28, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 3, 27, 0, 0, 0, 0, DateTimeKind.Local) });

            migrationBuilder.UpdateData(
                table: "StockMasters",
                keyColumn: "Id",
                keyValue: 1,
                column: "ReorderPoint",
                value: null);

            migrationBuilder.UpdateData(
                table: "StockMasters",
                keyColumn: "Id",
                keyValue: 2,
                column: "ReorderPoint",
                value: null);

            migrationBuilder.UpdateData(
                table: "StockMasters",
                keyColumn: "Id",
                keyValue: 3,
                column: "ReorderPoint",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReorderPoint",
                table: "StockMasters");

            migrationBuilder.UpdateData(
                table: "IssOrders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeliveryDate", "OrderDate" },
                values: new object[] { new DateTime(2026, 3, 26, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 3, 25, 0, 0, 0, 0, DateTimeKind.Local) });
        }
    }
}
