using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Faz2_NetsisIntegrationScaffold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NetsisStockCode",
                table: "StockMasters",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NetsisCariKodu",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "IssOrders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeliveryDate", "OrderDate" },
                values: new object[] { new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 3, 22, 0, 0, 0, 0, DateTimeKind.Local) });

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: 1,
                column: "NetsisCariKodu",
                value: null);

            migrationBuilder.UpdateData(
                table: "StockMasters",
                keyColumn: "Id",
                keyValue: 1,
                column: "NetsisStockCode",
                value: null);

            migrationBuilder.UpdateData(
                table: "StockMasters",
                keyColumn: "Id",
                keyValue: 2,
                column: "NetsisStockCode",
                value: null);

            migrationBuilder.UpdateData(
                table: "StockMasters",
                keyColumn: "Id",
                keyValue: 3,
                column: "NetsisStockCode",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NetsisStockCode",
                table: "StockMasters");

            migrationBuilder.DropColumn(
                name: "NetsisCariKodu",
                table: "Projects");

            migrationBuilder.UpdateData(
                table: "IssOrders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeliveryDate", "OrderDate" },
                values: new object[] { new DateTime(2026, 3, 19, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 3, 18, 0, 0, 0, 0, DateTimeKind.Local) });
        }
    }
}
