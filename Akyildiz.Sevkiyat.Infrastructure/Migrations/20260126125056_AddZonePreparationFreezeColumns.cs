using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddZonePreparationFreezeColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ZonePreparations_ZoneId_DeliveryDate",
                table: "ZonePreparations");

            migrationBuilder.AddColumn<int>(
                name: "BatchNo",
                table: "ZonePreparations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsFrozen",
                table: "ZonePreparations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "ZonePreparations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StartedByUserId",
                table: "ZonePreparations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ZonePreparationId",
                table: "Shipments",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "IssOrders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeliveryDate", "OrderDate" },
                values: new object[] { new DateTime(2026, 1, 27, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 1, 26, 0, 0, 0, 0, DateTimeKind.Local) });

            migrationBuilder.CreateIndex(
                name: "IX_ZonePreparations_ZoneId_DeliveryDate_BatchNo",
                table: "ZonePreparations",
                columns: new[] { "ZoneId", "DeliveryDate", "BatchNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_ZonePreparationId",
                table: "Shipments",
                column: "ZonePreparationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Shipments_ZonePreparations_ZonePreparationId",
                table: "Shipments",
                column: "ZonePreparationId",
                principalTable: "ZonePreparations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Shipments_ZonePreparations_ZonePreparationId",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_ZonePreparations_ZoneId_DeliveryDate_BatchNo",
                table: "ZonePreparations");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_ZonePreparationId",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "BatchNo",
                table: "ZonePreparations");

            migrationBuilder.DropColumn(
                name: "IsFrozen",
                table: "ZonePreparations");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "ZonePreparations");

            migrationBuilder.DropColumn(
                name: "StartedByUserId",
                table: "ZonePreparations");

            migrationBuilder.DropColumn(
                name: "ZonePreparationId",
                table: "Shipments");

            migrationBuilder.UpdateData(
                table: "IssOrders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeliveryDate", "OrderDate" },
                values: new object[] { new DateTime(2026, 1, 20, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 1, 19, 0, 0, 0, 0, DateTimeKind.Local) });

            migrationBuilder.CreateIndex(
                name: "IX_ZonePreparations_ZoneId_DeliveryDate",
                table: "ZonePreparations",
                columns: new[] { "ZoneId", "DeliveryDate" },
                unique: true);
        }
    }
}
