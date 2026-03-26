using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddZonePreparationEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ZonePreparations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ZoneId = table.Column<int>(type: "int", nullable: false),
                    DeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DriverName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlateNumber = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZonePreparations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ZonePreparations_Zones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "Zones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ZonePreparationProjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ZonePreparationId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    IsMicroReady = table.Column<bool>(type: "bit", nullable: false),
                    MicroReadyAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZonePreparationProjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ZonePreparationProjects_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ZonePreparationProjects_ZonePreparations_ZonePreparationId",
                        column: x => x.ZonePreparationId,
                        principalTable: "ZonePreparations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ZonePreparationProjects_ProjectId",
                table: "ZonePreparationProjects",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ZonePreparationProjects_ZonePreparationId",
                table: "ZonePreparationProjects",
                column: "ZonePreparationId");

            migrationBuilder.CreateIndex(
                name: "IX_ZonePreparations_ZoneId_DeliveryDate",
                table: "ZonePreparations",
                columns: new[] { "ZoneId", "DeliveryDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ZonePreparationProjects");

            migrationBuilder.DropTable(
                name: "ZonePreparations");
        }
    }
}
