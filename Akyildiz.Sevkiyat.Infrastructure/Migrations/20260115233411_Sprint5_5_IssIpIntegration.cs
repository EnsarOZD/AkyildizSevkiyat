using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Sprint5_5_IssIpIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_StockMasters",
                table: "StockMasters");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "StockMasters",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "ImportStatus",
                table: "IssOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_StockMasters",
                table: "StockMasters",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "StockMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalSystem = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExternalStockCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExternalStockName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LocalStockId = table.Column<int>(type: "int", nullable: true),
                    MatchStatus = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMappings_StockMasters_LocalStockId",
                        column: x => x.LocalStockId,
                        principalTable: "StockMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.UpdateData(
                table: "IssOrders",
                keyColumn: "Id",
                keyValue: 1,
                column: "ImportStatus",
                value: 0);

            migrationBuilder.CreateIndex(
                name: "IX_StockMasters_StockCode",
                table: "StockMasters",
                column: "StockCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMappings_LocalStockId",
                table: "StockMappings",
                column: "LocalStockId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockMappings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StockMasters",
                table: "StockMasters");

            migrationBuilder.DropIndex(
                name: "IX_StockMasters_StockCode",
                table: "StockMasters");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "StockMasters");

            migrationBuilder.DropColumn(
                name: "ImportStatus",
                table: "IssOrders");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StockMasters",
                table: "StockMasters",
                column: "StockCode");
        }
    }
}
