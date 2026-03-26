using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DeduplicateProjectsAndUniqueCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Step 1: Deduplicate ──────────────────────────────────────────────────
            // For each Code group keep the row with MIN(Id) as canonical.
            // Re-point all FK references to the canonical row, then delete duplicates.

            // ZonePreparationProjects: drop rows that would create a (ZonePreparationId, ProjectId)
            // collision after re-pointing (both canonical and duplicate in same preparation).
            migrationBuilder.Sql(@"
                DELETE zpp
                FROM ZonePreparationProjects zpp
                INNER JOIN Projects dup ON zpp.ProjectId = dup.Id
                INNER JOIN (SELECT Code, MIN(Id) AS CanonicalId FROM Projects GROUP BY Code) grp
                    ON dup.Code = grp.Code AND dup.Id <> grp.CanonicalId
                WHERE EXISTS (
                    SELECT 1 FROM ZonePreparationProjects zpp2
                    WHERE zpp2.ZonePreparationId = zpp.ZonePreparationId
                      AND zpp2.ProjectId = grp.CanonicalId
                )
            ");

            // Re-point remaining ZonePreparationProjects
            migrationBuilder.Sql(@"
                UPDATE zpp
                SET zpp.ProjectId = grp.CanonicalId
                FROM ZonePreparationProjects zpp
                INNER JOIN Projects dup ON zpp.ProjectId = dup.Id
                INNER JOIN (SELECT Code, MIN(Id) AS CanonicalId FROM Projects GROUP BY Code) grp
                    ON dup.Code = grp.Code AND dup.Id <> grp.CanonicalId
            ");

            // Re-point IssOrders
            migrationBuilder.Sql(@"
                UPDATE o
                SET o.ProjectId = grp.CanonicalId
                FROM IssOrders o
                INNER JOIN Projects dup ON o.ProjectId = dup.Id
                INNER JOIN (SELECT Code, MIN(Id) AS CanonicalId FROM Projects GROUP BY Code) grp
                    ON dup.Code = grp.Code AND dup.Id <> grp.CanonicalId
            ");

            // Re-point Shipments
            migrationBuilder.Sql(@"
                UPDATE s
                SET s.ProjectId = grp.CanonicalId
                FROM Shipments s
                INNER JOIN Projects dup ON s.ProjectId = dup.Id
                INNER JOIN (SELECT Code, MIN(Id) AS CanonicalId FROM Projects GROUP BY Code) grp
                    ON dup.Code = grp.Code AND dup.Id <> grp.CanonicalId
            ");

            // Delete duplicate projects (keep MIN(Id) per Code)
            migrationBuilder.Sql(@"
                DELETE FROM Projects
                WHERE Id NOT IN (SELECT MIN(Id) FROM Projects GROUP BY Code)
            ");

            // ── Step 2: Narrow column to allow unique index ──────────────────────────
            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Projects",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // ── Step 3: Unique index ─────────────────────────────────────────────────
            migrationBuilder.CreateIndex(
                name: "IX_Projects_Code",
                table: "Projects",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Projects_Code",
                table: "Projects");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
