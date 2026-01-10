using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sobee.Domain.Migrations.Sobee
{
    /// <inheritdoc />
    public partial class AddIndexesAndFixPromotionPk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TCartItems_intShoppingCartID",
                table: "TCartItems");

            migrationBuilder.CreateIndex(
                name: "IX_TShoppingCarts_session_id",
                table: "TShoppingCarts",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_TShoppingCarts_user_id",
                table: "TShoppingCarts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_TOrders_session_id",
                table: "TOrders",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_TOrders_user_id",
                table: "TOrders",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "UX_TFavorites_user_product",
                table: "TFavorites",
                columns: new[] { "user_id", "intProductID" },
                unique: true,
                filter: "[user_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TCartItems_cart_product",
                table: "TCartItems",
                columns: new[] { "intShoppingCartID", "intProductID" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TShoppingCarts_session_id",
                table: "TShoppingCarts");

            migrationBuilder.DropIndex(
                name: "IX_TShoppingCarts_user_id",
                table: "TShoppingCarts");

            migrationBuilder.DropIndex(
                name: "IX_TOrders_session_id",
                table: "TOrders");

            migrationBuilder.DropIndex(
                name: "IX_TOrders_user_id",
                table: "TOrders");

            migrationBuilder.DropIndex(
                name: "UX_TFavorites_user_product",
                table: "TFavorites");

            migrationBuilder.DropIndex(
                name: "IX_TCartItems_cart_product",
                table: "TCartItems");

            migrationBuilder.CreateIndex(
                name: "IX_TCartItems_intShoppingCartID",
                table: "TCartItems",
                column: "intShoppingCartID");
        }
    }
}
