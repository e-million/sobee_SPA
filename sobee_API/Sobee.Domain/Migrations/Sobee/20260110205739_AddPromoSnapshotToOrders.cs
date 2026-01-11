using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sobee.Domain.Migrations.Sobee
{
    /// <inheritdoc />
    public partial class AddPromoSnapshotToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "decDiscountAmount",
                table: "TOrders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "decDiscountPercentage",
                table: "TOrders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "decSubtotalAmount",
                table: "TOrders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "strPromoCode",
                table: "TOrders",
                type: "varchar(255)",
                unicode: false,
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "decDiscountAmount",
                table: "TOrders");

            migrationBuilder.DropColumn(
                name: "decDiscountPercentage",
                table: "TOrders");

            migrationBuilder.DropColumn(
                name: "decSubtotalAmount",
                table: "TOrders");

            migrationBuilder.DropColumn(
                name: "strPromoCode",
                table: "TOrders");
        }
    }
}
