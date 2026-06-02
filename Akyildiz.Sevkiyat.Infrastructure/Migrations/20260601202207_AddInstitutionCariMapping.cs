using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInstitutionCariMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InstitutionCariMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstitutionCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NetsisCariKodu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstitutionCariMappings", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "IssOrders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeliveryDate", "OrderDate" },
                values: new object[] { new DateTime(2026, 6, 2, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Local) });

            migrationBuilder.CreateIndex(
                name: "IX_InstitutionCariMappings_InstitutionCode",
                table: "InstitutionCariMappings",
                column: "InstitutionCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InstitutionCariMappings");

            migrationBuilder.UpdateData(
                table: "IssOrders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeliveryDate", "OrderDate" },
                values: new object[] { new DateTime(2026, 5, 24, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 5, 23, 0, 0, 0, 0, DateTimeKind.Local) });
        }
    }
}
