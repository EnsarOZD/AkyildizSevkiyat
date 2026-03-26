using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IssOrderUniqueIndex_ImportBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── IssOrder.ExternalOrderNumber unique index ──────────────────────────

            // Step 1: deduplicate — keep the row with the lowest Id
            migrationBuilder.Sql(@"
                DELETE FROM IssOrderLines
                WHERE IssOrderId IN (
                    SELECT Id FROM IssOrders
                    WHERE Id NOT IN (
                        SELECT MIN(Id) FROM IssOrders GROUP BY ExternalOrderNumber
                    )
                );

                DELETE FROM IssOrders
                WHERE Id NOT IN (
                    SELECT MIN(Id) FROM IssOrders GROUP BY ExternalOrderNumber
                );
            ");

            // Step 2: alter column to nvarchar(100) (required for unique index in SQL Server)
            migrationBuilder.AlterColumn<string>(
                name: "ExternalOrderNumber",
                table: "IssOrders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // Step 3: create unique index
            migrationBuilder.CreateIndex(
                name: "IX_IssOrders_ExternalOrderNumber",
                table: "IssOrders",
                column: "ExternalOrderNumber",
                unique: true);

            // ── ImportBatch tables ─────────────────────────────────────────────────

            migrationBuilder.CreateTable(
                name: "ImportBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "ISS-IP"),
                    RequestedStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestedEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartedByUserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalFromSource = table.Column<int>(type: "int", nullable: false),
                    NewCount = table.Column<int>(type: "int", nullable: false),
                    SkippedCount = table.Column<int>(type: "int", nullable: false),
                    NeedsMappingCount = table.Column<int>(type: "int", nullable: false),
                    FailedCount = table.Column<int>(type: "int", nullable: false),
                    DurationMs = table.Column<int>(type: "int", nullable: false),
                    ErrorSummary = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportBatchOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImportBatchId = table.Column<int>(type: "int", nullable: false),
                    ExternalOrderNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Warning = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Error = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IssOrderId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportBatchOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportBatchOrders_ImportBatches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "ImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatchOrders_ImportBatchId",
                table: "ImportBatchOrders",
                column: "ImportBatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ImportBatchOrders");
            migrationBuilder.DropTable(name: "ImportBatches");

            migrationBuilder.DropIndex(
                name: "IX_IssOrders_ExternalOrderNumber",
                table: "IssOrders");

            migrationBuilder.AlterColumn<string>(
                name: "ExternalOrderNumber",
                table: "IssOrders",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
