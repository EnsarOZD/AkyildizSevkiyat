using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemSettingsAndDeliveryWindows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "DeliveryWindowEnd",
                table: "Projects",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "DeliveryWindowStart",
                table: "Projects",
                type: "time",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepotName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DepotAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DepotLatitude = table.Column<double>(type: "float", nullable: true),
                    DepotLongitude = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeliveryWindowEnd", "DeliveryWindowStart" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "DeliveryWindowEnd",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "DeliveryWindowStart",
                table: "Projects");
        }
    }
}
