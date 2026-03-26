using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Sprint5_ProjectBasedZoning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMasters_Zones_ZoneId",
                table: "StockMasters");

            migrationBuilder.DropIndex(
                name: "IX_StockMasters_ZoneId",
                table: "StockMasters");

            migrationBuilder.DropColumn(
                name: "ZoneId",
                table: "StockMasters");

            migrationBuilder.AddColumn<int>(
                name: "ZoneId",
                table: "Projects",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: 1,
                column: "ZoneId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ZoneId",
                table: "Projects",
                column: "ZoneId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Zones_ZoneId",
                table: "Projects",
                column: "ZoneId",
                principalTable: "Zones",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Zones_ZoneId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_ZoneId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ZoneId",
                table: "Projects");

            migrationBuilder.AddColumn<int>(
                name: "ZoneId",
                table: "StockMasters",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMasters_ZoneId",
                table: "StockMasters",
                column: "ZoneId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMasters_Zones_ZoneId",
                table: "StockMasters",
                column: "ZoneId",
                principalTable: "Zones",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
