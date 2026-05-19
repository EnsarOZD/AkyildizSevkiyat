using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMacroPickingLock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "MacroLockedAt",
                table: "ZonePreparations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MacroLockedByUserId",
                table: "ZonePreparations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MacroLockedByUserName",
                table: "ZonePreparations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "IssOrders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeliveryDate", "OrderDate" },
                values: new object[] { new DateTime(2026, 5, 7, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 5, 6, 0, 0, 0, 0, DateTimeKind.Local) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MacroLockedAt",
                table: "ZonePreparations");

            migrationBuilder.DropColumn(
                name: "MacroLockedByUserId",
                table: "ZonePreparations");

            migrationBuilder.DropColumn(
                name: "MacroLockedByUserName",
                table: "ZonePreparations");

            migrationBuilder.UpdateData(
                table: "IssOrders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeliveryDate", "OrderDate" },
                values: new object[] { new DateTime(2026, 5, 6, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 5, 5, 0, 0, 0, 0, DateTimeKind.Local) });
        }
    }
}
