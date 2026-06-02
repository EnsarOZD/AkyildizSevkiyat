using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddManualCustomerSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Shipments_IssOrders_IssOrderId",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_IssOrderId",
                table: "Shipments");

            migrationBuilder.AlterColumn<int>(
                name: "IssOrderId",
                table: "Shipments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "Projects",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "IssOrders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeliveryDate", "OrderDate" },
                values: new object[] { new DateTime(2026, 5, 24, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 5, 23, 0, 0, 0, 0, DateTimeKind.Local) });

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: 1,
                column: "Source",
                value: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_IssOrderId",
                table: "Shipments",
                column: "IssOrderId",
                unique: true,
                filter: "[IssOrderId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Shipments_IssOrders_IssOrderId",
                table: "Shipments",
                column: "IssOrderId",
                principalTable: "IssOrders",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Shipments_IssOrders_IssOrderId",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_IssOrderId",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Projects");

            migrationBuilder.AlterColumn<int>(
                name: "IssOrderId",
                table: "Shipments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "IssOrders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeliveryDate", "OrderDate" },
                values: new object[] { new DateTime(2026, 5, 19, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 5, 18, 0, 0, 0, 0, DateTimeKind.Local) });

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_IssOrderId",
                table: "Shipments",
                column: "IssOrderId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Shipments_IssOrders_IssOrderId",
                table: "Shipments",
                column: "IssOrderId",
                principalTable: "IssOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
