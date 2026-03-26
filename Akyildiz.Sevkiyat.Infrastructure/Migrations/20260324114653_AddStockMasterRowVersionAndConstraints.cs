using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStockMasterRowVersionAndConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "StockMasters",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMaster_OnHandQty_NonNegative",
                table: "StockMasters",
                sql: "[OnHandQty] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMaster_ReservedLteOnHand",
                table: "StockMasters",
                sql: "[ReservedQty] <= [OnHandQty]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMaster_ReservedQty_NonNegative",
                table: "StockMasters",
                sql: "[ReservedQty] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMaster_OnHandQty_NonNegative",
                table: "StockMasters");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMaster_ReservedLteOnHand",
                table: "StockMasters");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMaster_ReservedQty_NonNegative",
                table: "StockMasters");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "StockMasters");
        }
    }
}
