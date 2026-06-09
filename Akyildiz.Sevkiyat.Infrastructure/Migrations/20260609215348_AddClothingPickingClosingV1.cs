using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClothingPickingClosingV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignedPickerId",
                table: "Shipments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedPickerName",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BoxCount",
                table: "Shipments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClaimedAt",
                table: "Shipments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ClaimedOutOfOrder",
                table: "Shipments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                table: "Shipments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClosedByUserName",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LabelPrinted",
                table: "Shipments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LabelPrintedAt",
                table: "Shipments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PackageType",
                table: "Shipments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PalletCount",
                table: "Shipments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PickingCompletedAt",
                table: "Shipments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PickingGroupId",
                table: "Shipments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PickingMode",
                table: "Shipments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PickingPausedAt",
                table: "Shipments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QueueOrder",
                table: "Shipments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReservedForUserId",
                table: "Shipments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Containers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Containers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PickingGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PickingGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShortageRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShipmentId = table.Column<int>(type: "int", nullable: false),
                    ShipmentLineId = table.Column<int>(type: "int", nullable: true),
                    StockMasterId = table.Column<int>(type: "int", nullable: true),
                    StockCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StockName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    ProjectName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Qty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedByUserId = table.Column<int>(type: "int", nullable: true),
                    FollowupShipmentId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShortageRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShortageRecords_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContainerAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContainerId = table.Column<int>(type: "int", nullable: false),
                    ShipmentId = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReleasedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssignedByUserId = table.Column<int>(type: "int", nullable: true),
                    ReleaseReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContainerAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContainerAssignments_Containers_ContainerId",
                        column: x => x.ContainerId,
                        principalTable: "Containers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContainerAssignments_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "IssOrders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeliveryDate", "OrderDate" },
                values: new object[] { new DateTime(2026, 6, 11, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 6, 10, 0, 0, 0, 0, DateTimeKind.Local) });

            migrationBuilder.CreateIndex(
                name: "IX_ContainerAssignments_ContainerId",
                table: "ContainerAssignments",
                column: "ContainerId");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerAssignments_ShipmentId",
                table: "ContainerAssignments",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Containers_Code",
                table: "Containers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShortageRecords_ShipmentId",
                table: "ShortageRecords",
                column: "ShipmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContainerAssignments");

            migrationBuilder.DropTable(
                name: "PickingGroups");

            migrationBuilder.DropTable(
                name: "ShortageRecords");

            migrationBuilder.DropTable(
                name: "Containers");

            migrationBuilder.DropColumn(
                name: "AssignedPickerId",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "AssignedPickerName",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "BoxCount",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "ClaimedAt",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "ClaimedOutOfOrder",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "ClosedByUserName",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "LabelPrinted",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "LabelPrintedAt",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "PackageType",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "PalletCount",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "PickingCompletedAt",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "PickingGroupId",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "PickingMode",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "PickingPausedAt",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "QueueOrder",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "ReservedForUserId",
                table: "Shipments");

            migrationBuilder.UpdateData(
                table: "IssOrders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeliveryDate", "OrderDate" },
                values: new object[] { new DateTime(2026, 6, 10, 0, 0, 0, 0, DateTimeKind.Local), new DateTime(2026, 6, 9, 0, 0, 0, 0, DateTimeKind.Local) });
        }
    }
}
