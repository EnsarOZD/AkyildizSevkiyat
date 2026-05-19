using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotoStorageColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveryPhotoPath",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EndOdometerPhotoPath",
                table: "DriverSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StartOdometerPhotoPath",
                table: "DriverSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "IssOrders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeliveryDate", "OrderDate" },
                values: new object[] { new DateTime(2026, 5, 5, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 5, 4, 0, 0, 0, 0, DateTimeKind.Local) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryPhotoPath",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "EndOdometerPhotoPath",
                table: "DriverSessions");

            migrationBuilder.DropColumn(
                name: "StartOdometerPhotoPath",
                table: "DriverSessions");

            migrationBuilder.UpdateData(
                table: "IssOrders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeliveryDate", "OrderDate" },
                values: new object[] { new DateTime(2026, 4, 26, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 4, 25, 0, 0, 0, 0, DateTimeKind.Local) });
        }
    }
}
