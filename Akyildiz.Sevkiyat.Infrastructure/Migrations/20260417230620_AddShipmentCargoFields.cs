using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShipmentCargoFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CargoProvider",
                table: "Shipments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CargoTrackingNumber",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PrintedByName",
                table: "ShipmentPrintLogs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.UpdateData(
                table: "IssOrders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeliveryDate", "OrderDate" },
                values: new object[] { new DateTime(2026, 4, 19, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 4, 18, 0, 0, 0, 0, DateTimeKind.Local) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CargoProvider",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "CargoTrackingNumber",
                table: "Shipments");

            migrationBuilder.AlterColumn<string>(
                name: "PrintedByName",
                table: "ShipmentPrintLogs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.UpdateData(
                table: "IssOrders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeliveryDate", "OrderDate" },
                values: new object[] { new DateTime(2026, 4, 12, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 4, 11, 0, 0, 0, 0, DateTimeKind.Local) });
        }
    }
}
