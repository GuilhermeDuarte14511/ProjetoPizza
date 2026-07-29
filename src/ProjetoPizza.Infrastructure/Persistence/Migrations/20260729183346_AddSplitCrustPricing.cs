using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoPizza.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSplitCrustPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "half_additional_price",
                schema: "catalog",
                table: "pizza_crust_prices",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("""
                UPDATE catalog.pizza_crust_prices
                SET half_additional_price = ROUND(additional_price / 2, 2);
                """);

            migrationBuilder.AddColumn<string>(
                name: "crust_selection_mode",
                schema: "ordering",
                table: "order_item_pizzas",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.Sql("""
                UPDATE ordering.order_item_pizzas
                SET crust_selection_mode = 'Whole'
                WHERE pizza_crust_id IS NOT NULL;
                """);

            migrationBuilder.AddColumn<string>(
                name: "second_crust_name_snapshot",
                schema: "ordering",
                table: "order_item_pizzas",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "second_pizza_crust_id",
                schema: "ordering",
                table: "order_item_pizzas",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_order_item_pizzas_second_pizza_crust_id",
                schema: "ordering",
                table: "order_item_pizzas",
                column: "second_pizza_crust_id");

            migrationBuilder.AddForeignKey(
                name: "fk_order_item_pizzas_pizza_crusts_second_pizza_crust_id",
                schema: "ordering",
                table: "order_item_pizzas",
                column: "second_pizza_crust_id",
                principalSchema: "catalog",
                principalTable: "pizza_crusts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_order_item_pizzas_pizza_crusts_second_pizza_crust_id",
                schema: "ordering",
                table: "order_item_pizzas");

            migrationBuilder.DropIndex(
                name: "ix_order_item_pizzas_second_pizza_crust_id",
                schema: "ordering",
                table: "order_item_pizzas");

            migrationBuilder.DropColumn(
                name: "half_additional_price",
                schema: "catalog",
                table: "pizza_crust_prices");

            migrationBuilder.DropColumn(
                name: "crust_selection_mode",
                schema: "ordering",
                table: "order_item_pizzas");

            migrationBuilder.DropColumn(
                name: "second_crust_name_snapshot",
                schema: "ordering",
                table: "order_item_pizzas");

            migrationBuilder.DropColumn(
                name: "second_pizza_crust_id",
                schema: "ordering",
                table: "order_item_pizzas");
        }
    }
}
