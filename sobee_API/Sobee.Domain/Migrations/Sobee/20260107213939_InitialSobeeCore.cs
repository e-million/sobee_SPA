using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sobee.Domain.Migrations.Sobee
{
    /// <inheritdoc />
    public partial class InitialSobeeCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TDrinkCategories",
                columns: table => new
                {
                    intDrinkCategoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    strName = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    strDescription = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("TDrinkCategories_PK", x => x.intDrinkCategoryID);
                });

            migrationBuilder.CreateTable(
                name: "TFlavors",
                columns: table => new
                {
                    intFlavorID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    strFlavor = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("TFlavors_PK", x => x.intFlavorID);
                });

            migrationBuilder.CreateTable(
                name: "TIngredients",
                columns: table => new
                {
                    intIngredientID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    strIngredient = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("TIngredients_PK", x => x.intIngredientID);
                });

            migrationBuilder.CreateTable(
                name: "TOrdersProducts",
                columns: table => new
                {
                    intOrdersProductID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    intProductID = table.Column<int>(type: "int", nullable: false),
                    strOrdersProduct = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("TOrdersProducts_PK", x => x.intOrdersProductID);
                });

            migrationBuilder.CreateTable(
                name: "TPaymentMethods",
                columns: table => new
                {
                    intPaymentMethodID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    strCreditCardDetails = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    strBillingAddress = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    strDescription = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("TPaymentMethods_PK", x => x.intPaymentMethodID);
                });

            migrationBuilder.CreateTable(
                name: "TProductImages",
                columns: table => new
                {
                    intProductImageID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    strProductImageURL = table.Column<string>(type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("TProductImages_PK", x => x.intProductImageID);
                });

            migrationBuilder.CreateTable(
                name: "TProducts",
                columns: table => new
                {
                    intProductID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    strName = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    strDescription = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    decPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    strStockAmount = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("TProducts_PK", x => x.intProductID);
                });

            migrationBuilder.CreateTable(
                name: "TPromotions",
                columns: table => new
                {
                    intPromotionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    strPromoCode = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    strDiscountPercentage = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    dtmExpirationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    decDiscountPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("TPromotions_PK", x => x.intPromotionID);
                });

            migrationBuilder.CreateTable(
                name: "TShippingMethods",
                columns: table => new
                {
                    intShippingMethodID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    strShippingName = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    strBillingAddress = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    dtmEstimatedDelivery = table.Column<DateTime>(type: "datetime", nullable: false),
                    strCost = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("TShippingMethods_PK", x => x.intShippingMethodID);
                });

            migrationBuilder.CreateTable(
                name: "TShippingStatus",
                columns: table => new
                {
                    intShippingStatusID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    strShippingStatus = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("TShippingStatus_PK", x => x.intShippingStatusID);
                });

            migrationBuilder.CreateTable(
                name: "TShoppingCarts",
                columns: table => new
                {
                    intShoppingCartID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    dtmDateCreated = table.Column<DateTime>(type: "datetime", nullable: true),
                    dtmDateLastUpdated = table.Column<DateTime>(type: "datetime", nullable: true),
                    user_id = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    session_id = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("TShoppingCarts_PK", x => x.intShoppingCartID);
                });

            migrationBuilder.CreateTable(
                name: "TTransactionTypes",
                columns: table => new
                {
                    intTransactionTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    strTransactionType = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("TTransactionTypes_PK", x => x.intTransactionTypeID);
                });

            migrationBuilder.CreateTable(
                name: "TPayments",
                columns: table => new
                {
                    intPaymentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    strBillingAddress = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    intPaymentMethodID = table.Column<int>(type: "int", nullable: true),
                    intPaymentMethod = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("TPayments_PK", x => x.intPaymentID);
                    table.ForeignKey(
                        name: "TPayments_TPaymentMethods_FK",
                        column: x => x.intPaymentMethodID,
                        principalTable: "TPaymentMethods",
                        principalColumn: "intPaymentMethodID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TFavorites",
                columns: table => new
                {
                    intFavoriteID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    intProductID = table.Column<int>(type: "int", nullable: false),
                    dtmDateAdded = table.Column<DateTime>(type: "datetime", nullable: false),
                    user_id = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("TFavorites_PK", x => x.intFavoriteID);
                    table.ForeignKey(
                        name: "TFavorites_TProducts_FK",
                        column: x => x.intProductID,
                        principalTable: "TProducts",
                        principalColumn: "intProductID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TReviews",
                columns: table => new
                {
                    intReviewID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    intProductID = table.Column<int>(type: "int", nullable: false),
                    strReviewText = table.Column<string>(type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: false),
                    intRating = table.Column<int>(type: "int", nullable: false),
                    dtmReviewDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    user_id = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    session_id = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("TReviews_PK", x => x.intReviewID);
                    table.ForeignKey(
                        name: "TReviews_TProducts_FK",
                        column: x => x.intProductID,
                        principalTable: "TProducts",
                        principalColumn: "intProductID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TOrders",
                columns: table => new
                {
                    intOrderID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    dtmOrderDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    decTotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    intShippingStatusID = table.Column<int>(type: "int", nullable: true),
                    strShippingAddress = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    strTrackingNumber = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    intPaymentMethodID = table.Column<int>(type: "int", nullable: true),
                    strOrderStatus = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    user_id = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    session_id = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("TOrders_PK", x => x.intOrderID);
                    table.ForeignKey(
                        name: "TOrders_TPaymentMethods_FK",
                        column: x => x.intPaymentMethodID,
                        principalTable: "TPaymentMethods",
                        principalColumn: "intPaymentMethodID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "TOrders_TShippingStatus_FK",
                        column: x => x.intShippingStatusID,
                        principalTable: "TShippingStatus",
                        principalColumn: "intShippingStatusID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TCartItems",
                columns: table => new
                {
                    intCartItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    intShoppingCartID = table.Column<int>(type: "int", nullable: true),
                    intProductID = table.Column<int>(type: "int", nullable: true),
                    intQuantity = table.Column<int>(type: "int", nullable: true),
                    dtmDateAdded = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("TCartItems_PK", x => x.intCartItemID);
                    table.ForeignKey(
                        name: "TCartItems_TProducts_FK",
                        column: x => x.intProductID,
                        principalTable: "TProducts",
                        principalColumn: "intProductID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "TCartItems_TShoppingCarts_FK",
                        column: x => x.intShoppingCartID,
                        principalTable: "TShoppingCarts",
                        principalColumn: "intShoppingCartID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TPromoCodeUsageHistory",
                columns: table => new
                {
                    intUsageHistoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    intShoppingCartID = table.Column<int>(type: "int", nullable: false),
                    PromoCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    UsedDateTime = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("TPromoCodeUsageHistory_PK", x => x.intUsageHistoryID);
                    table.ForeignKey(
                        name: "TPromoCodeUsageHistory_TShoppingCarts_FK",
                        column: x => x.intShoppingCartID,
                        principalTable: "TShoppingCarts",
                        principalColumn: "intShoppingCartID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TReviewReplies",
                columns: table => new
                {
                    intReviewReplyID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    review_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("TReviewReplies_PK", x => x.intReviewReplyID);
                    table.ForeignKey(
                        name: "FK_ReviewReplies_Reviews",
                        column: x => x.review_id,
                        principalTable: "TReviews",
                        principalColumn: "intReviewID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TOrderItems",
                columns: table => new
                {
                    intOrderItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    intOrderID = table.Column<int>(type: "int", nullable: true),
                    intProductID = table.Column<int>(type: "int", nullable: true),
                    intQuantity = table.Column<int>(type: "int", nullable: true),
                    monPricePerUnit = table.Column<decimal>(type: "money", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("TOrderItems_PK", x => x.intOrderItemID);
                    table.ForeignKey(
                        name: "TOrderItems_TOrders_FK",
                        column: x => x.intOrderID,
                        principalTable: "TOrders",
                        principalColumn: "intOrderID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "TOrderItems_TProducts_FK",
                        column: x => x.intProductID,
                        principalTable: "TProducts",
                        principalColumn: "intProductID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TCartItems_intProductID",
                table: "TCartItems",
                column: "intProductID");

            migrationBuilder.CreateIndex(
                name: "IX_TCartItems_intShoppingCartID",
                table: "TCartItems",
                column: "intShoppingCartID");

            migrationBuilder.CreateIndex(
                name: "IX_TFavorites_intProductID",
                table: "TFavorites",
                column: "intProductID");

            migrationBuilder.CreateIndex(
                name: "IX_TOrderItems_intOrderID",
                table: "TOrderItems",
                column: "intOrderID");

            migrationBuilder.CreateIndex(
                name: "IX_TOrderItems_intProductID",
                table: "TOrderItems",
                column: "intProductID");

            migrationBuilder.CreateIndex(
                name: "IX_TOrders_intPaymentMethodID",
                table: "TOrders",
                column: "intPaymentMethodID");

            migrationBuilder.CreateIndex(
                name: "IX_TOrders_intShippingStatusID",
                table: "TOrders",
                column: "intShippingStatusID");

            migrationBuilder.CreateIndex(
                name: "IX_TPayments_intPaymentMethodID",
                table: "TPayments",
                column: "intPaymentMethodID");

            migrationBuilder.CreateIndex(
                name: "IX_TPromoCodeUsageHistory_intShoppingCartID",
                table: "TPromoCodeUsageHistory",
                column: "intShoppingCartID");

            migrationBuilder.CreateIndex(
                name: "IX_TReviewReplies_review_id",
                table: "TReviewReplies",
                column: "review_id");

            migrationBuilder.CreateIndex(
                name: "IX_TReviews_intProductID",
                table: "TReviews",
                column: "intProductID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TCartItems");

            migrationBuilder.DropTable(
                name: "TDrinkCategories");

            migrationBuilder.DropTable(
                name: "TFavorites");

            migrationBuilder.DropTable(
                name: "TFlavors");

            migrationBuilder.DropTable(
                name: "TIngredients");

            migrationBuilder.DropTable(
                name: "TOrderItems");

            migrationBuilder.DropTable(
                name: "TOrdersProducts");

            migrationBuilder.DropTable(
                name: "TPayments");

            migrationBuilder.DropTable(
                name: "TProductImages");

            migrationBuilder.DropTable(
                name: "TPromoCodeUsageHistory");

            migrationBuilder.DropTable(
                name: "TPromotions");

            migrationBuilder.DropTable(
                name: "TReviewReplies");

            migrationBuilder.DropTable(
                name: "TShippingMethods");

            migrationBuilder.DropTable(
                name: "TTransactionTypes");

            migrationBuilder.DropTable(
                name: "TOrders");

            migrationBuilder.DropTable(
                name: "TShoppingCarts");

            migrationBuilder.DropTable(
                name: "TReviews");

            migrationBuilder.DropTable(
                name: "TPaymentMethods");

            migrationBuilder.DropTable(
                name: "TShippingStatus");

            migrationBuilder.DropTable(
                name: "TProducts");
        }
    }
}
