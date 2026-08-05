using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoPizza.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomersAndAdministrativeOrdering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "customers");

            migrationBuilder.AddColumn<string>(
                name: "customer_name_snapshot",
                schema: "ordering",
                table: "orders",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "delivery_address_snapshot",
                schema: "ordering",
                table: "orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "customers",
                schema: "customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    phone = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customers", x => x.id);
                    table.ForeignKey(
                        name: "fk_customers_restaurant_units_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "core",
                        principalTable: "restaurant_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_orders_customer_id",
                schema: "ordering",
                table: "orders",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_customers_unit_id_name",
                schema: "customers",
                table: "customers",
                columns: new[] { "unit_id", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_customers_unit_id_phone",
                schema: "customers",
                table: "customers",
                columns: new[] { "unit_id", "phone" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_orders_customers_customer_id",
                schema: "ordering",
                table: "orders",
                column: "customer_id",
                principalSchema: "customers",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_orders_customers_customer_id",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropTable(
                name: "customers",
                schema: "customers");

            migrationBuilder.DropIndex(
                name: "ix_orders_customer_id",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "customer_name_snapshot",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "delivery_address_snapshot",
                schema: "ordering",
                table: "orders");
        }
    }
}
