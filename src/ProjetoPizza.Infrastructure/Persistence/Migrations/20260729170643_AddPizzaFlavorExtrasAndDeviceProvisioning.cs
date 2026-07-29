using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoPizza.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPizzaFlavorExtrasAndDeviceProvisioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "device_provisionings",
                schema: "devices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_device_provisionings", x => x.id);
                    table.ForeignKey(
                        name: "fk_device_provisionings_devices_device_id",
                        column: x => x.device_id,
                        principalSchema: "devices",
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pizza_flavor_extras",
                schema: "catalog",
                columns: table => new
                {
                    pizza_flavor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ingredient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    max_quantity = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pizza_flavor_extras", x => new { x.pizza_flavor_id, x.ingredient_id });
                    table.ForeignKey(
                        name: "fk_pizza_flavor_extras_ingredients_ingredient_id",
                        column: x => x.ingredient_id,
                        principalSchema: "catalog",
                        principalTable: "ingredients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pizza_flavor_extras_pizza_flavors_pizza_flavor_id",
                        column: x => x.pizza_flavor_id,
                        principalSchema: "catalog",
                        principalTable: "pizza_flavors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_device_provisionings_device_id_expires_at",
                schema: "devices",
                table: "device_provisionings",
                columns: new[] { "device_id", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_device_provisionings_token_hash",
                schema: "devices",
                table: "device_provisionings",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pizza_flavor_extras_ingredient_id",
                schema: "catalog",
                table: "pizza_flavor_extras",
                column: "ingredient_id");

            migrationBuilder.CreateIndex(
                name: "ix_pizza_flavor_extras_pizza_flavor_id_is_active",
                schema: "catalog",
                table: "pizza_flavor_extras",
                columns: new[] { "pizza_flavor_id", "is_active" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "device_provisionings",
                schema: "devices");

            migrationBuilder.DropTable(
                name: "pizza_flavor_extras",
                schema: "catalog");
        }
    }
}
