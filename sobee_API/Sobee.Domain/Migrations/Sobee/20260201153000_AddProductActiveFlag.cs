using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Sobee.Domain.Data;

#nullable disable

namespace Sobee.Domain.Migrations.Sobee
{
    /// <inheritdoc />
    [DbContext(typeof(SobeecoredbContext))]
    [Migration("20260201153000_AddProductActiveFlag")]
    public partial class AddProductActiveFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "blnIsActive",
                table: "TProducts",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "blnIsActive",
                table: "TProducts");
        }
    }
}
