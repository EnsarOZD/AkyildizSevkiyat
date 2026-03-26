using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Region = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IssOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalOrderNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NetsisOrderNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IssOrders_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IssOrderLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IssOrderId = table.Column<int>(type: "int", nullable: false),
                    LineNumber = table.Column<int>(type: "int", nullable: false),
                    StockCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StockName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrderedQty = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssOrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IssOrderLines_IssOrders_IssOrderId",
                        column: x => x.IssOrderId,
                        principalTable: "IssOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Shipments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    DeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IssOrderId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Shipments_IssOrders_IssOrderId",
                        column: x => x.IssOrderId,
                        principalTable: "IssOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Shipments_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShipmentId = table.Column<int>(type: "int", nullable: false),
                    IssOrderLineId = table.Column<int>(type: "int", nullable: false),
                    StockCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StockName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrderedQty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DeliveredQty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DifferenceReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentLines_IssOrderLines_IssOrderLineId",
                        column: x => x.IssOrderLineId,
                        principalTable: "IssOrderLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShipmentLines_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Projects",
                columns: new[] { "Id", "Address", "Code", "IsActive", "Name", "Region" },
                values: new object[] { 1, "İstanbul / Kozyatağı", "PRJ001", true, "Kozyatağı Yemekhanesi", "Anadolu-1" });

            migrationBuilder.InsertData(
                table: "IssOrders",
                columns: new[] { "Id", "DeliveryDate", "ExternalOrderNumber", "NetsisOrderNumber", "OrderDate", "ProjectId", "Status" },
                values: new object[] { 1, new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Local), "SO-1001", null, new DateTime(2026, 1, 14, 0, 0, 0, 0, DateTimeKind.Local), 1, "Imported" });

            migrationBuilder.InsertData(
                table: "IssOrderLines",
                columns: new[] { "Id", "IssOrderId", "LineNumber", "OrderedQty", "StockCode", "StockName", "Unit" },
                values: new object[] { 1, 1, 1, 120m, "EKMEK-01", "Somun Ekmek", "Adet" });

            migrationBuilder.CreateIndex(
                name: "IX_IssOrderLines_IssOrderId",
                table: "IssOrderLines",
                column: "IssOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_IssOrders_ProjectId",
                table: "IssOrders",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentLines_IssOrderLineId",
                table: "ShipmentLines",
                column: "IssOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentLines_ShipmentId",
                table: "ShipmentLines",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_IssOrderId",
                table: "Shipments",
                column: "IssOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_ProjectId",
                table: "Shipments",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShipmentLines");

            migrationBuilder.DropTable(
                name: "IssOrderLines");

            migrationBuilder.DropTable(
                name: "Shipments");

            migrationBuilder.DropTable(
                name: "IssOrders");

            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
