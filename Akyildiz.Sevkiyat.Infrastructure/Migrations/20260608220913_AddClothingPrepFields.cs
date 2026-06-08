using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClothingPrepFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClothingType",
                table: "StockMasters",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KoliCount",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PreparedAt",
                table: "Shipments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreparedByUserName",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClothingHighlightKeywords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Keyword = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClothingHighlightKeywords", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "IssOrders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeliveryDate", "OrderDate" },
                values: new object[] { new DateTime(2026, 6, 10, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 6, 9, 0, 0, 0, 0, DateTimeKind.Local) });

            migrationBuilder.UpdateData(
                table: "StockMasters",
                keyColumn: "Id",
                keyValue: 1,
                column: "ClothingType",
                value: null);

            migrationBuilder.UpdateData(
                table: "StockMasters",
                keyColumn: "Id",
                keyValue: 2,
                column: "ClothingType",
                value: null);

            migrationBuilder.UpdateData(
                table: "StockMasters",
                keyColumn: "Id",
                keyValue: 3,
                column: "ClothingType",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClothingHighlightKeywords");

            migrationBuilder.DropColumn(
                name: "ClothingType",
                table: "StockMasters");

            migrationBuilder.DropColumn(
                name: "KoliCount",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "PreparedAt",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "PreparedByUserName",
                table: "Shipments");

            migrationBuilder.UpdateData(
                table: "IssOrders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeliveryDate", "OrderDate" },
                values: new object[] { new DateTime(2026, 6, 6, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 6, 5, 0, 0, 0, 0, DateTimeKind.Local) });
        }
    }
}
