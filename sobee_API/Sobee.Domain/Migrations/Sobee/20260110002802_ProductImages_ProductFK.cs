using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sobee.Domain.Migrations.Sobee
{
    /// <inheritdoc />
    public partial class ProductImages_ProductFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "intProductID",
                table: "TProductImages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TProductImages_intProductID",
                table: "TProductImages",
                column: "intProductID");

            migrationBuilder.AddForeignKey(
                name: "FK_TProductImages_TProducts_intProductID",
                table: "TProductImages",
                column: "intProductID",
                principalTable: "TProducts",
                principalColumn: "intProductID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TProductImages_TProducts_intProductID",
                table: "TProductImages");

            migrationBuilder.DropIndex(
                name: "IX_TProductImages_intProductID",
                table: "TProductImages");

            migrationBuilder.DropColumn(
                name: "intProductID",
                table: "TProductImages");
        }
    }
}
