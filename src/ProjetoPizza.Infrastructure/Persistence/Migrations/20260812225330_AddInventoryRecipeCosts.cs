using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoPizza.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryRecipeCosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "unit_cost",
                schema: "inventory",
                table: "inventory_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "unit_cost",
                schema: "inventory",
                table: "inventory_items");
        }
    }
}
