using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase3_DriverIdentityHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignedDriverId",
                table: "Shipments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveredByRole",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveredByUserId",
                table: "Shipments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryOverrideNote",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Drivers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_AssignedDriverId",
                table: "Shipments",
                column: "AssignedDriverId");

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_UserId",
                table: "Drivers",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Drivers_Users_UserId",
                table: "Drivers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Shipments_Drivers_AssignedDriverId",
                table: "Shipments",
                column: "AssignedDriverId",
                principalTable: "Drivers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Drivers_Users_UserId",
                table: "Drivers");

            migrationBuilder.DropForeignKey(
                name: "FK_Shipments_Drivers_AssignedDriverId",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_AssignedDriverId",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_Drivers_UserId",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "AssignedDriverId",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DeliveredByRole",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DeliveredByUserId",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DeliveryOverrideNote",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Drivers");
        }
    }
}
