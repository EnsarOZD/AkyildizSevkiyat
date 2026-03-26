using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReconciliationIssues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReconciliationIssues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IssueKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CheckType = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ShipmentId = table.Column<int>(type: "int", nullable: true),
                    ShipmentLineId = table.Column<int>(type: "int", nullable: true),
                    IssOrderLineId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ExpectedValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ActualValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcknowledgedByUserId = table.Column<int>(type: "int", nullable: true),
                    AcknowledgedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcknowledgementNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationIssues", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationIssues_CheckType",
                table: "ReconciliationIssues",
                column: "CheckType");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationIssues_IssueKey",
                table: "ReconciliationIssues",
                column: "IssueKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationIssues_Status_DetectedAt",
                table: "ReconciliationIssues",
                columns: new[] { "Status", "DetectedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReconciliationIssues");
        }
    }
}
