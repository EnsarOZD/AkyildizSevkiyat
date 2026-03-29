using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixSystemSettingsNoIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQL Server cannot alter a column to remove IDENTITY in-place.
            // Recreate the table without the identity annotation.
            migrationBuilder.DropTable(name: "SystemSettings");

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    DepotName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DepotAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DepotLatitude = table.Column<double>(type: "float", nullable: true),
                    DepotLongitude = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SystemSettings");

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
        }
    }
}
