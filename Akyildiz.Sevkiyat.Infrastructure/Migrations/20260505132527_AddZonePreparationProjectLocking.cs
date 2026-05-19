using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddZonePreparationProjectLocking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PickingLockedAt",
                table: "ZonePreparationProjects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PickingLockedByUserId",
                table: "ZonePreparationProjects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PickingLockedByUserName",
                table: "ZonePreparationProjects",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreparedByUserName",
                table: "ZonePreparationProjects",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PickingLockedAt",
                table: "ZonePreparationProjects");

            migrationBuilder.DropColumn(
                name: "PickingLockedByUserId",
                table: "ZonePreparationProjects");

            migrationBuilder.DropColumn(
                name: "PickingLockedByUserName",
                table: "ZonePreparationProjects");

            migrationBuilder.DropColumn(
                name: "PreparedByUserName",
                table: "ZonePreparationProjects");
        }
    }
}
