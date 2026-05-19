using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddYkExtendedFieldsToShipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "YkErrorCode",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "YkErrorMessage",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "YkInvoiceKey",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "YkLastQueryAt",
                table: "Shipments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "YkOperationMessage",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "YkOperationStatus",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "YkErrorCode",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "YkErrorMessage",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "YkInvoiceKey",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "YkLastQueryAt",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "YkOperationMessage",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "YkOperationStatus",
                table: "Shipments");
        }
    }
}
