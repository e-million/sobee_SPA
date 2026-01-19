using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sobee.Domain.Migrations.Sobee
{
    /// <inheritdoc />
    public partial class AddAnalyticsPhase2Fields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "decCost",
                table: "TProducts",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "intDrinkCategoryID",
                table: "TProducts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "dtmDeliveredDate",
                table: "TOrders",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "dtmShippedDate",
                table: "TOrders",
                type: "datetime",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TProducts_intDrinkCategoryID",
                table: "TProducts",
                column: "intDrinkCategoryID");

            migrationBuilder.AddForeignKey(
                name: "TProducts_TDrinkCategories_FK",
                table: "TProducts",
                column: "intDrinkCategoryID",
                principalTable: "TDrinkCategories",
                principalColumn: "intDrinkCategoryID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "TProducts_TDrinkCategories_FK",
                table: "TProducts");

            migrationBuilder.DropIndex(
                name: "IX_TProducts_intDrinkCategoryID",
                table: "TProducts");

            migrationBuilder.DropColumn(
                name: "decCost",
                table: "TProducts");

            migrationBuilder.DropColumn(
                name: "intDrinkCategoryID",
                table: "TProducts");

            migrationBuilder.DropColumn(
                name: "dtmDeliveredDate",
                table: "TOrders");

            migrationBuilder.DropColumn(
                name: "dtmShippedDate",
                table: "TOrders");
        }
    }
}
