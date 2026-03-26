using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDispatchedStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new columns for dispatch confirmation
            // Already added in AddShipmentDispatchFields migration, just altering type
            migrationBuilder.AlterColumn<string>(
                name: "DispatchConfirmedByName",
                table: "Shipments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            // Data migration: shift existing Status integer values to make room for Dispatched=5
            // Old mapping: Delivered=5, Cancelled=6, ReturnedToWarehouse=7
            // New mapping: Dispatched=5, Delivered=6, Cancelled=7, ReturnedToWarehouse=8
            // Process from highest to lowest to avoid value collisions
            migrationBuilder.Sql("UPDATE Shipments SET Status = 8 WHERE Status = 7"); // ReturnedToWarehouse 7→8
            migrationBuilder.Sql("UPDATE Shipments SET Status = 7 WHERE Status = 6"); // Cancelled 6→7
            migrationBuilder.Sql("UPDATE Shipments SET Status = 6 WHERE Status = 5"); // Delivered 5→6
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse data migration (process from lowest to highest)
            migrationBuilder.Sql("UPDATE Shipments SET Status = 5 WHERE Status = 6"); // Delivered 6→5
            migrationBuilder.Sql("UPDATE Shipments SET Status = 6 WHERE Status = 7"); // Cancelled 7→6
            migrationBuilder.Sql("UPDATE Shipments SET Status = 7 WHERE Status = 8"); // ReturnedToWarehouse 8→7

            // Revert AlterColumn for DispatchConfirmedByName back to nvarchar(max)
            migrationBuilder.AlterColumn<string>(
                name: "DispatchConfirmedByName",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}
