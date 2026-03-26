using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WarehouseLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Zone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Row = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Rack = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Slot = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LocationType = table.Column<int>(type: "int", nullable: false),
                    MaxWeightKg = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    MaxPallets = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseLocations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocations_Code",
                table: "WarehouseLocations",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WarehouseLocations");
        }
    }
}
