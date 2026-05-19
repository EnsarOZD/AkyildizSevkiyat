using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOdometerKmToDriverSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EndOdometerKm",
                table: "DriverSessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StartOdometerKm",
                table: "DriverSessions",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndOdometerKm",
                table: "DriverSessions");

            migrationBuilder.DropColumn(
                name: "StartOdometerKm",
                table: "DriverSessions");
        }
    }
}
