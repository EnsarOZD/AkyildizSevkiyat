using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedStocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "StockMasters",
                columns: new[] { "Id", "IsActive", "StockCode", "StockName" },
                values: new object[,]
                {
                    { 1, true, "EKMEK-01", "Somun Ekmek" },
                    { 2, true, "SU-05", "Su 0.5L" },
                    { 3, true, "YOGURT-200", "Yoğurt 200gr" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "StockMasters",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "StockMasters",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "StockMasters",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}
