using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixStockDecimalPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TaxRate",
                table: "StockMasters",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "StockMasters",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "StockMasters",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "StockMasters",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "TaxRate", "Unit", "UnitPrice" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "StockMasters",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "TaxRate", "Unit", "UnitPrice" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "StockMasters",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "TaxRate", "Unit", "UnitPrice" },
                values: new object[] { null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaxRate",
                table: "StockMasters");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "StockMasters");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "StockMasters");
        }
    }
}
