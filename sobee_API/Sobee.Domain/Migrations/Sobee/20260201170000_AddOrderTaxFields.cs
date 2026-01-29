using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Sobee.Domain.Data;

#nullable disable

namespace Sobee.Domain.Migrations.Sobee
{
    /// <inheritdoc />
    [DbContext(typeof(SobeecoredbContext))]
    [Migration("20260201170000_AddOrderTaxFields")]
    public partial class AddOrderTaxFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "decTaxAmount",
                table: "TOrders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "decTaxRate",
                table: "TOrders",
                type: "decimal(18,4)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "decTaxAmount",
                table: "TOrders");

            migrationBuilder.DropColumn(
                name: "decTaxRate",
                table: "TOrders");
        }
    }
}
