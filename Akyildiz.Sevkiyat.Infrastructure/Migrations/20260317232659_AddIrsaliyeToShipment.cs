using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIrsaliyeToShipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "IrsaliyeDate",
                table: "Shipments",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IrsaliyeNo",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NetsisTransferredAt",
                table: "Shipments",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IrsaliyeDate",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "IrsaliyeNo",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "NetsisTransferredAt",
                table: "Shipments");
        }
    }
}
