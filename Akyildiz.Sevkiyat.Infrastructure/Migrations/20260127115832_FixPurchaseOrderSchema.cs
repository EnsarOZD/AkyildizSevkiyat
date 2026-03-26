using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Akyildiz.Sevkiyat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPurchaseOrderSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Clear broken test data to avoid FK conflicts
            migrationBuilder.Sql("DELETE FROM PurchaseOrderLines; DELETE FROM PurchaseOrders; DELETE FROM GoodsReceiptLines; DELETE FROM GoodsReceipts;");

            // Robust rename
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.columns WHERE Name = 'OrderNo' AND Object_ID = Object_ID('PurchaseOrders')) EXEC sp_rename 'PurchaseOrders.OrderNo', 'OrderNumber', 'COLUMN'");

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplierId",
                table: "PurchaseOrders",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SupplierId",
                table: "GoodsReceipts",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OrderNumber",
                table: "PurchaseOrders",
                newName: "OrderNo");

            migrationBuilder.AlterColumn<string>(
                name: "SupplierId",
                table: "PurchaseOrders",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "SupplierId",
                table: "GoodsReceipts",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");
        }
    }
}
