using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIssOrderExtendedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Aciklama",
                table: "IssOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Donem",
                table: "IssOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "IssOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TalepNo",
                table: "IssOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TalepTuru",
                table: "IssOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TeslimAlacakKisiler",
                table: "IssOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TeslimAlacakTelefonNumaralari",
                table: "IssOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "YoneticiMailAdresleri",
                table: "IssOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BirimFiyati",
                table: "IssOrderLines",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Iskonto",
                table: "IssOrderLines",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "KDVOrani",
                table: "IssOrderLines",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ListeFiyati",
                table: "IssOrderLines",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "IssOrderLines",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "BirimFiyati", "Iskonto", "KDVOrani", "ListeFiyati" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "IssOrders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Aciklama", "DeliveryDate", "Donem", "IsActive", "OrderDate", "TalepNo", "TalepTuru", "TeslimAlacakKisiler", "TeslimAlacakTelefonNumaralari", "YoneticiMailAdresleri" },
                values: new object[] { null, new DateTime(2026, 1, 19, 0, 0, 0, 0, DateTimeKind.Local), null, true, new DateTime(2026, 1, 18, 0, 0, 0, 0, DateTimeKind.Local), null, null, null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Aciklama",
                table: "IssOrders");

            migrationBuilder.DropColumn(
                name: "Donem",
                table: "IssOrders");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "IssOrders");

            migrationBuilder.DropColumn(
                name: "TalepNo",
                table: "IssOrders");

            migrationBuilder.DropColumn(
                name: "TalepTuru",
                table: "IssOrders");

            migrationBuilder.DropColumn(
                name: "TeslimAlacakKisiler",
                table: "IssOrders");

            migrationBuilder.DropColumn(
                name: "TeslimAlacakTelefonNumaralari",
                table: "IssOrders");

            migrationBuilder.DropColumn(
                name: "YoneticiMailAdresleri",
                table: "IssOrders");

            migrationBuilder.DropColumn(
                name: "BirimFiyati",
                table: "IssOrderLines");

            migrationBuilder.DropColumn(
                name: "Iskonto",
                table: "IssOrderLines");

            migrationBuilder.DropColumn(
                name: "KDVOrani",
                table: "IssOrderLines");

            migrationBuilder.DropColumn(
                name: "ListeFiyati",
                table: "IssOrderLines");

            migrationBuilder.UpdateData(
                table: "IssOrders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeliveryDate", "OrderDate" },
                values: new object[] { new DateTime(2026, 1, 18, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 1, 17, 0, 0, 0, 0, DateTimeKind.Local) });
        }
    }
}
