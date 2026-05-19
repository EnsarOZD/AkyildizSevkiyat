using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGidaHazirlikStatusAndPickingOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ZonePreparationStatus enum değerleri kaydırıldı:
            // Eski: ReadyForDriverInfo=4, ReadyForTransfer=5, Dispatched=6
            // Yeni: GidaHazirlik=4 (YENİ), ReadyForDriverInfo=5, ReadyForTransfer=6, Dispatched=7
            migrationBuilder.Sql(
                "UPDATE ZonePreparations SET Status = Status + 1 WHERE Status >= 4");

            migrationBuilder.AddColumn<int>(
                name: "PickingOrder",
                table: "StockMasters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "StockMasters",
                keyColumn: "Id",
                keyValue: 1,
                column: "PickingOrder",
                value: 0);

            migrationBuilder.UpdateData(
                table: "StockMasters",
                keyColumn: "Id",
                keyValue: 2,
                column: "PickingOrder",
                value: 0);

            migrationBuilder.UpdateData(
                table: "StockMasters",
                keyColumn: "Id",
                keyValue: 3,
                column: "PickingOrder",
                value: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Status değerlerini eski haline döndür (GidaHazirlik=4 aşamasındakiler MacroPicking=3'e döner)
            migrationBuilder.Sql(
                "UPDATE ZonePreparations SET Status = Status - 1 WHERE Status >= 5");
            migrationBuilder.Sql(
                "UPDATE ZonePreparations SET Status = 3 WHERE Status = 4"); // GidaHazirlik → MacroPicking

            migrationBuilder.DropColumn(
                name: "PickingOrder",
                table: "StockMasters");
        }
    }
}
