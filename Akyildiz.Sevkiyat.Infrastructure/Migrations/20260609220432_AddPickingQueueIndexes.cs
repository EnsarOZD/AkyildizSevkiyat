using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPickingQueueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Shipments_AssignedPickerId",
                table: "Shipments",
                column: "AssignedPickerId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_PickingGroupId_QueueOrder",
                table: "Shipments",
                columns: new[] { "PickingGroupId", "QueueOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Shipments_AssignedPickerId",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_PickingGroupId_QueueOrder",
                table: "Shipments");
        }
    }
}
