using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoPizza.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPizzaIngredientExtras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_ingredients_unit_id",
                schema: "catalog",
                table: "ingredients");

            migrationBuilder.AddColumn<decimal>(
                name: "extra_price",
                schema: "catalog",
                table: "ingredients",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "is_available_as_extra",
                schema: "catalog",
                table: "ingredients",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "max_extra_quantity",
                schema: "catalog",
                table: "ingredients",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "ix_ingredients_unit_id_is_active_is_available_as_extra",
                schema: "catalog",
                table: "ingredients",
                columns: new[] { "unit_id", "is_active", "is_available_as_extra" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_ingredients_unit_id_is_active_is_available_as_extra",
                schema: "catalog",
                table: "ingredients");

            migrationBuilder.DropColumn(
                name: "extra_price",
                schema: "catalog",
                table: "ingredients");

            migrationBuilder.DropColumn(
                name: "is_available_as_extra",
                schema: "catalog",
                table: "ingredients");

            migrationBuilder.DropColumn(
                name: "max_extra_quantity",
                schema: "catalog",
                table: "ingredients");

            migrationBuilder.CreateIndex(
                name: "ix_ingredients_unit_id",
                schema: "catalog",
                table: "ingredients",
                column: "unit_id");
        }
    }
}
