using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverSessionOpenUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ensures a driver cannot have more than one Open session at a time.
            // Filtered to Status = 0 (Open) so closed/force-closed sessions are not affected.
            migrationBuilder.CreateIndex(
                name: "IX_DriverSessions_Driver_OneOpen",
                table: "DriverSessions",
                column: "DriverId",
                unique: true,
                filter: "[Status] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DriverSessions_Driver_OneOpen",
                table: "DriverSessions");
        }
    }
}
