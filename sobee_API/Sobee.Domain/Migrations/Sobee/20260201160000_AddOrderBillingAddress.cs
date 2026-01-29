using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Sobee.Domain.Data;

#nullable disable

namespace Sobee.Domain.Migrations.Sobee
{
    /// <inheritdoc />
    [DbContext(typeof(SobeecoredbContext))]
    [Migration("20260201160000_AddOrderBillingAddress")]
    public partial class AddOrderBillingAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "strBillingAddress",
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
                name: "strBillingAddress",
                table: "TOrders");
        }
    }
}
