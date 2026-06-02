using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFreightDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FreightDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    CarrierName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CarrierPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RecipientName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FreightDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FreightDeliveries_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FreightDeliveryShipments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FreightDeliveryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShipmentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FreightDeliveryShipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FreightDeliveryShipments_FreightDeliveries_FreightDeliveryId",
                        column: x => x.FreightDeliveryId,
                        principalTable: "FreightDeliveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FreightDeliveryShipments_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FreightDeliveries_ProjectId",
                table: "FreightDeliveries",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_FreightDeliveries_Token",
                table: "FreightDeliveries",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FreightDeliveryShipments_FreightDeliveryId_ShipmentId",
                table: "FreightDeliveryShipments",
                columns: new[] { "FreightDeliveryId", "ShipmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FreightDeliveryShipments_ShipmentId",
                table: "FreightDeliveryShipments",
                column: "ShipmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FreightDeliveryShipments");

            migrationBuilder.DropTable(
                name: "FreightDeliveries");
        }
    }
}
