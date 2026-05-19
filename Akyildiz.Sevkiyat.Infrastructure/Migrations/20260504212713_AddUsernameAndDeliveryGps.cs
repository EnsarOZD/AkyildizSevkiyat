using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUsernameAndDeliveryGps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Users",
                type: "nvarchar(100)",
                nullable: false,
                defaultValue: "");

            // Populate existing users' username from email
            migrationBuilder.Sql("UPDATE Users SET Username = Email WHERE Username = ''");

            migrationBuilder.AddColumn<double>(
                name: "DeliveryLatitude",
                table: "Shipments",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DeliveryLongitude",
                table: "Shipments",
                type: "float",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "IssOrders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeliveryDate", "OrderDate" },
                values: new object[] { new DateTime(2026, 5, 6, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 5, 5, 0, 0, 0, 0, DateTimeKind.Local) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Username",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeliveryLatitude",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DeliveryLongitude",
                table: "Shipments");

            migrationBuilder.UpdateData(
                table: "IssOrders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeliveryDate", "OrderDate" },
                values: new object[] { new DateTime(2026, 5, 5, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 5, 4, 0, 0, 0, 0, DateTimeKind.Local) });
        }
    }
}
