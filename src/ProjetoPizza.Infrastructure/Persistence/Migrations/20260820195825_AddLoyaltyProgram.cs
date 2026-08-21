using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoPizza.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLoyaltyProgram : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "coupon_code",
                schema: "ordering",
                table: "orders",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "coupon_discount",
                schema: "ordering",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "loyalty_discount",
                schema: "ordering",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "loyalty_points_redeemed",
                schema: "ordering",
                table: "orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "manual_discount",
                schema: "ordering",
                table: "orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "promotion_coupon_id",
                schema: "ordering",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "loyalty_points_expire_at",
                schema: "customers",
                table: "customers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE ordering.orders
                SET manual_discount = discount;

                UPDATE customers.customers
                SET loyalty_points_expire_at = NOW() + INTERVAL '365 days'
                WHERE loyalty_points > 0 AND loyalty_points_expire_at IS NULL;
                """);

            migrationBuilder.CreateTable(
                name: "loyalty_settings",
                schema: "customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    points_per_currency_unit = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    redemption_value_per_point = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    minimum_redemption_points = table.Column<int>(type: "integer", nullable: false),
                    maximum_redemption_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    points_validity_days = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loyalty_settings", x => x.id);
                    table.ForeignKey(
                        name: "fk_loyalty_settings_restaurant_units_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "core",
                        principalTable: "restaurant_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                INSERT INTO customers.loyalty_settings
                    (id, unit_id, is_enabled, points_per_currency_unit, redemption_value_per_point,
                     minimum_redemption_points, maximum_redemption_percentage, points_validity_days, updated_at)
                SELECT md5(unit.id::text || ':loyalty')::uuid, unit.id, TRUE, 1, 0.05, 100, 30, 365, NOW()
                FROM core.restaurant_units AS unit;
                """);

            migrationBuilder.CreateTable(
                name: "loyalty_transactions",
                schema: "customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    points = table.Column<int>(type: "integer", nullable: false),
                    balance_after = table.Column<int>(type: "integer", nullable: false),
                    discount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loyalty_transactions", x => x.id);
                    table.ForeignKey(
                        name: "fk_loyalty_transactions_customers_customer_id",
                        column: x => x.customer_id,
                        principalSchema: "customers",
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_loyalty_transactions_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "ordering",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "promotion_coupons",
                schema: "customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    discount_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    value = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    minimum_order_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    maximum_discount_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    usage_limit = table.Column<int>(type: "integer", nullable: true),
                    times_redeemed = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_promotion_coupons", x => x.id);
                    table.ForeignKey(
                        name: "fk_promotion_coupons_restaurant_units_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "core",
                        principalTable: "restaurant_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_orders_promotion_coupon_id",
                schema: "ordering",
                table: "orders",
                column: "promotion_coupon_id");

            migrationBuilder.CreateIndex(
                name: "ix_loyalty_settings_unit_id",
                schema: "customers",
                table: "loyalty_settings",
                column: "unit_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_loyalty_transactions_customer_id_occurred_at",
                schema: "customers",
                table: "loyalty_transactions",
                columns: new[] { "customer_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_loyalty_transactions_order_id_type",
                schema: "customers",
                table: "loyalty_transactions",
                columns: new[] { "order_id", "type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_promotion_coupons_unit_id_code",
                schema: "customers",
                table: "promotion_coupons",
                columns: new[] { "unit_id", "code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_orders_promotion_coupons_promotion_coupon_id",
                schema: "ordering",
                table: "orders",
                column: "promotion_coupon_id",
                principalSchema: "customers",
                principalTable: "promotion_coupons",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_orders_promotion_coupons_promotion_coupon_id",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropTable(
                name: "loyalty_settings",
                schema: "customers");

            migrationBuilder.DropTable(
                name: "loyalty_transactions",
                schema: "customers");

            migrationBuilder.DropTable(
                name: "promotion_coupons",
                schema: "customers");

            migrationBuilder.DropIndex(
                name: "ix_orders_promotion_coupon_id",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "coupon_code",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "coupon_discount",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "loyalty_discount",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "loyalty_points_redeemed",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "manual_discount",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "promotion_coupon_id",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "loyalty_points_expire_at",
                schema: "customers",
                table: "customers");
        }
    }
}
