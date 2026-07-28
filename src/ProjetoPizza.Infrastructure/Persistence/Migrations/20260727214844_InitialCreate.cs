using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ProjetoPizza.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.EnsureSchema(
                name: "billing");

            migrationBuilder.EnsureSchema(
                name: "cashier");

            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.EnsureSchema(
                name: "devices");

            migrationBuilder.EnsureSchema(
                name: "dining");

            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.EnsureSchema(
                name: "inventory");

            migrationBuilder.EnsureSchema(
                name: "production");

            migrationBuilder.EnsureSchema(
                name: "notifications");

            migrationBuilder.EnsureSchema(
                name: "core");

            migrationBuilder.EnsureSchema(
                name: "ordering");

            migrationBuilder.CreateTable(
                name: "restaurant_units",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    legal_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    trade_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    cnpj = table.Column<string>(type: "character varying(18)", maxLength: 18, nullable: false),
                    phone = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                    administrative_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    timezone = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_restaurant_units", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "service_call_types",
                schema: "dining",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_call_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cash_registers",
                schema: "cashier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cash_registers", x => x.id);
                    table.ForeignKey(
                        name: "fk_cash_registers_restaurant_units_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "core",
                        principalTable: "restaurant_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "categories",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    icon = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_visible_on_tablet = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_categories_categories_parent_category_id",
                        column: x => x.parent_category_id,
                        principalSchema: "catalog",
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_categories_restaurant_units_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "core",
                        principalTable: "restaurant_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "dining_areas",
                schema: "dining",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dining_areas", x => x.id);
                    table.ForeignKey(
                        name: "fk_dining_areas_restaurant_units_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "core",
                        principalTable: "restaurant_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    identity_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    display_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    phone = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                    employee_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_access_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employees", x => x.id);
                    table.ForeignKey(
                        name: "fk_employees_restaurant_units_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "core",
                        principalTable: "restaurant_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_items",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    sku = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    unit_of_measure = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    minimum_stock = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_inventory_items_restaurant_units_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "core",
                        principalTable: "restaurant_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                schema: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_notifications_restaurant_units_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "core",
                        principalTable: "restaurant_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "operation_settings",
                schema: "core",
                columns: table => new
                {
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allow_table_without_waiter = table.Column<bool>(type: "boolean", nullable: false),
                    allow_orders_without_open_cash_shift = table.Column<bool>(type: "boolean", nullable: false),
                    clear_tablet_after_table_close = table.Column<bool>(type: "boolean", nullable: false),
                    service_fee_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    default_delivery_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    delivery_order_sound_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    table_call_sound_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    table_call_tolerance_minutes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_operation_settings", x => x.unit_id);
                    table.ForeignKey(
                        name: "fk_operation_settings_restaurant_units_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "core",
                        principalTable: "restaurant_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_methods",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    requires_external_reference = table.Column<bool>(type: "boolean", nullable: false),
                    allows_change = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_methods", x => x.id);
                    table.ForeignKey(
                        name: "fk_payment_methods_restaurant_units_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "core",
                        principalTable: "restaurant_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pizza_crusts",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_available = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pizza_crusts", x => x.id);
                    table.ForeignKey(
                        name: "fk_pizza_crusts_restaurant_units_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "core",
                        principalTable: "restaurant_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pizza_settings",
                schema: "core",
                columns: table => new
                {
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    global_max_flavors = table.Column<int>(type: "integer", nullable: false),
                    pricing_policy = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    allow_sweet_and_savory_mix = table.Column<bool>(type: "boolean", nullable: false),
                    allow_extras_per_flavor = table.Column<bool>(type: "boolean", nullable: false),
                    allow_repeated_flavors = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pizza_settings", x => x.unit_id);
                    table.ForeignKey(
                        name: "fk_pizza_settings_restaurant_units_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "core",
                        principalTable: "restaurant_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pizza_sizes",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    short_name = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    slices = table.Column<int>(type: "integer", nullable: false),
                    diameter_cm = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    base_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    max_flavors = table.Column<int>(type: "integer", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pizza_sizes", x => x.id);
                    table.ForeignKey(
                        name: "fk_pizza_sizes_restaurant_units_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "core",
                        principalTable: "restaurant_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "production_stations",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    target_preparation_minutes = table.Column<int>(type: "integer", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_production_stations", x => x.id);
                    table.ForeignKey(
                        name: "fk_production_stations_restaurant_units_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "core",
                        principalTable: "restaurant_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_claims",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_claims_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "identity",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_claims",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_claims_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_logins",
                schema: "identity",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    provider_key = table.Column<string>(type: "text", nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_user_logins_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                schema: "identity",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "identity",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_tokens",
                schema: "identity",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_tokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_user_tokens_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pizza_flavors",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    flavor_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_premium = table.Column<bool>(type: "boolean", nullable: false),
                    is_vegetarian = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_available = table.Column<bool>(type: "boolean", nullable: false),
                    sold_out_reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    image_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pizza_flavors", x => x.id);
                    table.ForeignKey(
                        name: "fk_pizza_flavors_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "catalog",
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pizza_flavors_restaurant_units_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "core",
                        principalTable: "restaurant_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "products",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    product_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    base_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_available = table.Column<bool>(type: "boolean", nullable: false),
                    is_featured = table.Column<bool>(type: "boolean", nullable: false),
                    is_popular = table.Column<bool>(type: "boolean", nullable: false),
                    preparation_time_minutes = table.Column<int>(type: "integer", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.id);
                    table.ForeignKey(
                        name: "fk_products_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "catalog",
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_products_restaurant_units_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "core",
                        principalTable: "restaurant_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "restaurant_tables",
                schema: "dining",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dining_area_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_restaurant_tables", x => x.id);
                    table.ForeignKey(
                        name: "fk_restaurant_tables_dining_areas_dining_area_id",
                        column: x => x.dining_area_id,
                        principalSchema: "dining",
                        principalTable: "dining_areas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_restaurant_tables_restaurant_units_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "core",
                        principalTable: "restaurant_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    module = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_logs_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "identity",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_audit_logs_restaurant_units_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "core",
                        principalTable: "restaurant_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cash_shifts",
                schema: "cashier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cash_register_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operator_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    opened_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    opening_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    closed_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    expected_cash_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    counted_cash_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    difference_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    closing_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cash_shifts", x => x.id);
                    table.ForeignKey(
                        name: "fk_cash_shifts_cash_registers_cash_register_id",
                        column: x => x.cash_register_id,
                        principalSchema: "cashier",
                        principalTable: "cash_registers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cash_shifts_employees_closed_by_employee_id",
                        column: x => x.closed_by_employee_id,
                        principalSchema: "identity",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cash_shifts_employees_operator_employee_id",
                        column: x => x.operator_employee_id,
                        principalSchema: "identity",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "table_sessions",
                schema: "dining",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_number = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    guest_count = table.Column<int>(type: "integer", nullable: false),
                    primary_waiter_id = table.Column<Guid>(type: "uuid", nullable: true),
                    service_fee_percentage_snapshot = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    opened_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    opened_by_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bill_requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    closed_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_table_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_table_sessions_employees_closed_by_employee_id",
                        column: x => x.closed_by_employee_id,
                        principalSchema: "identity",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_table_sessions_employees_opened_by_employee_id",
                        column: x => x.opened_by_employee_id,
                        principalSchema: "identity",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_table_sessions_employees_primary_waiter_id",
                        column: x => x.primary_waiter_id,
                        principalSchema: "identity",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_table_sessions_restaurant_units_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "core",
                        principalTable: "restaurant_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ingredients",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    inventory_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_allergen = table.Column<bool>(type: "boolean", nullable: false),
                    allergen_description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ingredients", x => x.id);
                    table.ForeignKey(
                        name: "fk_ingredients_inventory_items_inventory_item_id",
                        column: x => x.inventory_item_id,
                        principalSchema: "inventory",
                        principalTable: "inventory_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ingredients_restaurant_units_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "core",
                        principalTable: "restaurant_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_balances",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inventory_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    reserved_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_balances", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_balances_inventory_items_inventory_item_id",
                        column: x => x.inventory_item_id,
                        principalSchema: "inventory",
                        principalTable: "inventory_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pizza_crust_prices",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pizza_crust_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pizza_size_id = table.Column<Guid>(type: "uuid", nullable: false),
                    additional_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pizza_crust_prices", x => x.id);
                    table.ForeignKey(
                        name: "fk_pizza_crust_prices_pizza_crusts_pizza_crust_id",
                        column: x => x.pizza_crust_id,
                        principalSchema: "catalog",
                        principalTable: "pizza_crusts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pizza_crust_prices_pizza_sizes_pizza_size_id",
                        column: x => x.pizza_size_id,
                        principalSchema: "catalog",
                        principalTable: "pizza_sizes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pizza_flavor_prices",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pizza_flavor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pizza_size_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    additional_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_available = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pizza_flavor_prices", x => x.id);
                    table.ForeignKey(
                        name: "fk_pizza_flavor_prices_pizza_flavors_pizza_flavor_id",
                        column: x => x.pizza_flavor_id,
                        principalSchema: "catalog",
                        principalTable: "pizza_flavors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pizza_flavor_prices_pizza_sizes_pizza_size_id",
                        column: x => x.pizza_size_id,
                        principalSchema: "catalog",
                        principalTable: "pizza_sizes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_images",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    alt_text = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_images", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_images_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "catalog",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_variants",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sku = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_available = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_variants", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_variants_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "catalog",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "devices",
                schema: "devices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    serial_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    device_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    platform = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    app_version = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    battery_percentage = table.Column<int>(type: "integer", nullable: true),
                    is_charging = table.Column<bool>(type: "boolean", nullable: false),
                    network_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    linked_table_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_devices", x => x.id);
                    table.ForeignKey(
                        name: "fk_devices_restaurant_tables_linked_table_id",
                        column: x => x.linked_table_id,
                        principalSchema: "dining",
                        principalTable: "restaurant_tables",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_devices_restaurant_units_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "core",
                        principalTable: "restaurant_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bills",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    table_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    service_fee_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    service_fee_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    paid_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    remaining_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bills", x => x.id);
                    table.ForeignKey(
                        name: "fk_bills_restaurant_units_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "core",
                        principalTable: "restaurant_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bills_table_sessions_table_session_id",
                        column: x => x.table_session_id,
                        principalSchema: "dining",
                        principalTable: "table_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "table_session_tables",
                schema: "dining",
                columns: table => new
                {
                    table_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    restaurant_table_id = table.Column<Guid>(type: "uuid", nullable: false),
                    linked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    unlinked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    linked_by_employee_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_table_session_tables", x => new { x.table_session_id, x.restaurant_table_id, x.linked_at });
                    table.ForeignKey(
                        name: "fk_table_session_tables_employees_linked_by_employee_id",
                        column: x => x.linked_by_employee_id,
                        principalSchema: "identity",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_table_session_tables_restaurant_tables_restaurant_table_id",
                        column: x => x.restaurant_table_id,
                        principalSchema: "dining",
                        principalTable: "restaurant_tables",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_table_session_tables_table_sessions_table_session_id",
                        column: x => x.table_session_id,
                        principalSchema: "dining",
                        principalTable: "table_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "waiter_assignments",
                schema: "dining",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    table_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    unassigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    assigned_by_employee_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_waiter_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_waiter_assignments_employees_assigned_by_employee_id",
                        column: x => x.assigned_by_employee_id,
                        principalSchema: "identity",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_waiter_assignments_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "identity",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_waiter_assignments_table_sessions_table_session_id",
                        column: x => x.table_session_id,
                        principalSchema: "dining",
                        principalTable: "table_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pizza_flavor_ingredients",
                schema: "catalog",
                columns: table => new
                {
                    pizza_flavor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ingredient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_of_measure = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_removable = table.Column<bool>(type: "boolean", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pizza_flavor_ingredients", x => new { x.pizza_flavor_id, x.ingredient_id });
                    table.ForeignKey(
                        name: "fk_pizza_flavor_ingredients_ingredients_ingredient_id",
                        column: x => x.ingredient_id,
                        principalSchema: "catalog",
                        principalTable: "ingredients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pizza_flavor_ingredients_pizza_flavors_pizza_flavor_id",
                        column: x => x.pizza_flavor_id,
                        principalSchema: "catalog",
                        principalTable: "pizza_flavors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "recipes",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pizza_flavor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pizza_size_id = table.Column<Guid>(type: "uuid", nullable: true),
                    yield_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recipes", x => x.id);
                    table.ForeignKey(
                        name: "fk_recipes_pizza_flavors_pizza_flavor_id",
                        column: x => x.pizza_flavor_id,
                        principalSchema: "catalog",
                        principalTable: "pizza_flavors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_recipes_pizza_sizes_pizza_size_id",
                        column: x => x.pizza_size_id,
                        principalSchema: "catalog",
                        principalTable: "pizza_sizes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_recipes_product_variants_product_variant_id",
                        column: x => x.product_variant_id,
                        principalSchema: "catalog",
                        principalTable: "product_variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_recipes_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "catalog",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "device_sessions",
                schema: "devices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    table_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    session_token_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ended_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_device_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_device_sessions_devices_device_id",
                        column: x => x.device_id,
                        principalSchema: "devices",
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_device_sessions_table_sessions_table_session_id",
                        column: x => x.table_session_id,
                        principalSchema: "dining",
                        principalTable: "table_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                schema: "ordering",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_number = table.Column<long>(type: "bigint", nullable: false),
                    table_session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sales_channel = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    fulfillment_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    payment_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    service_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    delivery_fee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    placed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_device_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_orders_devices_created_by_device_id",
                        column: x => x.created_by_device_id,
                        principalSchema: "devices",
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_orders_employees_created_by_employee_id",
                        column: x => x.created_by_employee_id,
                        principalSchema: "identity",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_orders_restaurant_units_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "core",
                        principalTable: "restaurant_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_orders_table_sessions_table_session_id",
                        column: x => x.table_session_id,
                        principalSchema: "dining",
                        principalTable: "table_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_calls",
                schema: "dining",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    table_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_call_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by_device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    assigned_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    acknowledged_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_calls", x => x.id);
                    table.ForeignKey(
                        name: "fk_service_calls_devices_requested_by_device_id",
                        column: x => x.requested_by_device_id,
                        principalSchema: "devices",
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_calls_employees_assigned_employee_id",
                        column: x => x.assigned_employee_id,
                        principalSchema: "identity",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_calls_restaurant_units_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "core",
                        principalTable: "restaurant_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_calls_service_call_types_service_call_type_id",
                        column: x => x.service_call_type_id,
                        principalSchema: "dining",
                        principalTable: "service_call_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_calls_table_sessions_table_session_id",
                        column: x => x.table_session_id,
                        principalSchema: "dining",
                        principalTable: "table_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bill_splits",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bill_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    split_number = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    paid_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    remaining_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bill_splits", x => x.id);
                    table.ForeignKey(
                        name: "fk_bill_splits_bills_bill_id",
                        column: x => x.bill_id,
                        principalSchema: "billing",
                        principalTable: "bills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "recipe_items",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inventory_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_of_measure = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recipe_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_recipe_items_inventory_items_inventory_item_id",
                        column: x => x.inventory_item_id,
                        principalSchema: "inventory",
                        principalTable: "inventory_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_recipe_items_recipes_recipe_id",
                        column: x => x.recipe_id,
                        principalSchema: "inventory",
                        principalTable: "recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "kitchen_tickets",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    production_station_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticket_number = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ready_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    dispatched_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_kitchen_tickets", x => x.id);
                    table.ForeignKey(
                        name: "fk_kitchen_tickets_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "ordering",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_kitchen_tickets_production_stations_production_station_id",
                        column: x => x.production_station_id,
                        principalSchema: "production",
                        principalTable: "production_stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_kitchen_tickets_restaurant_units_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "core",
                        principalTable: "restaurant_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                schema: "ordering",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    product_name_snapshot = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    variant_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    production_station_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sent_to_production_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ready_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    delivered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_items_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "ordering",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_items_product_variants_product_variant_id",
                        column: x => x.product_variant_id,
                        principalSchema: "catalog",
                        principalTable: "product_variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_items_production_stations_production_station_id",
                        column: x => x.production_station_id,
                        principalSchema: "production",
                        principalTable: "production_stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_items_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "catalog",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bill_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bill_split_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cash_shift_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payment_method_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    received_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    change_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    external_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    authorization_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    received_by_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_payments_bill_splits_bill_split_id",
                        column: x => x.bill_split_id,
                        principalSchema: "billing",
                        principalTable: "bill_splits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payments_bills_bill_id",
                        column: x => x.bill_id,
                        principalSchema: "billing",
                        principalTable: "bills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payments_cash_shifts_cash_shift_id",
                        column: x => x.cash_shift_id,
                        principalSchema: "cashier",
                        principalTable: "cash_shifts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payments_employees_received_by_employee_id",
                        column: x => x.received_by_employee_id,
                        principalSchema: "identity",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payments_payment_methods_payment_method_id",
                        column: x => x.payment_method_id,
                        principalSchema: "billing",
                        principalTable: "payment_methods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payments_restaurant_units_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "core",
                        principalTable: "restaurant_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bill_items",
                schema: "billing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bill_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    gross_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    service_fee_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    net_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bill_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_bill_items_bills_bill_id",
                        column: x => x.bill_id,
                        principalSchema: "billing",
                        principalTable: "bills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bill_items_order_items_order_item_id",
                        column: x => x.order_item_id,
                        principalSchema: "ordering",
                        principalTable: "order_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "kitchen_ticket_items",
                schema: "production",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kitchen_ticket_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_kitchen_ticket_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_kitchen_ticket_items_kitchen_tickets_kitchen_ticket_id",
                        column: x => x.kitchen_ticket_id,
                        principalSchema: "production",
                        principalTable: "kitchen_tickets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_kitchen_ticket_items_order_items_order_item_id",
                        column: x => x.order_item_id,
                        principalSchema: "ordering",
                        principalTable: "order_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_item_modifiers",
                schema: "ordering",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pizza_flavor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    modifier_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ingredient_id = table.Column<Guid>(type: "uuid", nullable: true),
                    option_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name_snapshot = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_item_modifiers", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_item_modifiers_ingredients_ingredient_id",
                        column: x => x.ingredient_id,
                        principalSchema: "catalog",
                        principalTable: "ingredients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_item_modifiers_order_items_order_item_id",
                        column: x => x.order_item_id,
                        principalSchema: "ordering",
                        principalTable: "order_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_item_modifiers_pizza_flavors_pizza_flavor_id",
                        column: x => x.pizza_flavor_id,
                        principalSchema: "catalog",
                        principalTable: "pizza_flavors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_item_pizzas",
                schema: "ordering",
                columns: table => new
                {
                    order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pizza_size_id = table.Column<Guid>(type: "uuid", nullable: false),
                    size_name_snapshot = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    slice_count_snapshot = table.Column<int>(type: "integer", nullable: false),
                    size_max_flavors = table.Column<int>(type: "integer", nullable: false),
                    pizza_crust_id = table.Column<Guid>(type: "uuid", nullable: true),
                    crust_name_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    flavor_count = table.Column<int>(type: "integer", nullable: false),
                    pricing_policy_snapshot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    base_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    crust_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    extras_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_item_pizzas", x => x.order_item_id);
                    table.ForeignKey(
                        name: "fk_order_item_pizzas_order_items_order_item_id",
                        column: x => x.order_item_id,
                        principalSchema: "ordering",
                        principalTable: "order_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_item_pizzas_pizza_crusts_pizza_crust_id",
                        column: x => x.pizza_crust_id,
                        principalSchema: "catalog",
                        principalTable: "pizza_crusts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_item_pizzas_pizza_sizes_pizza_size_id",
                        column: x => x.pizza_size_id,
                        principalSchema: "catalog",
                        principalTable: "pizza_sizes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_movements",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inventory_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    movement_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    order_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_movements", x => x.id);
                    table.ForeignKey(
                        name: "fk_stock_movements_employees_created_by_employee_id",
                        column: x => x.created_by_employee_id,
                        principalSchema: "identity",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_movements_inventory_items_inventory_item_id",
                        column: x => x.inventory_item_id,
                        principalSchema: "inventory",
                        principalTable: "inventory_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stock_movements_order_items_order_item_id",
                        column: x => x.order_item_id,
                        principalSchema: "ordering",
                        principalTable: "order_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cash_movements",
                schema: "cashier",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cash_shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    movement_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    created_by_employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    authorized_by_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cash_movements", x => x.id);
                    table.ForeignKey(
                        name: "fk_cash_movements_cash_shifts_cash_shift_id",
                        column: x => x.cash_shift_id,
                        principalSchema: "cashier",
                        principalTable: "cash_shifts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cash_movements_employees_authorized_by_employee_id",
                        column: x => x.authorized_by_employee_id,
                        principalSchema: "identity",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cash_movements_employees_created_by_employee_id",
                        column: x => x.created_by_employee_id,
                        principalSchema: "identity",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cash_movements_payments_payment_id",
                        column: x => x.payment_id,
                        principalSchema: "billing",
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bill_split_items",
                schema: "billing",
                columns: table => new
                {
                    bill_split_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bill_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    allocated_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bill_split_items", x => new { x.bill_split_id, x.bill_item_id });
                    table.ForeignKey(
                        name: "fk_bill_split_items_bill_items_bill_item_id",
                        column: x => x.bill_item_id,
                        principalSchema: "billing",
                        principalTable: "bill_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bill_split_items_bill_splits_bill_split_id",
                        column: x => x.bill_split_id,
                        principalSchema: "billing",
                        principalTable: "bill_splits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_item_pizza_flavors",
                schema: "ordering",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pizza_flavor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    flavor_name_snapshot = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    part_number = table.Column<int>(type: "integer", nullable: false),
                    total_parts = table.Column<int>(type: "integer", nullable: false),
                    calculated_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_item_pizza_flavors", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_item_pizza_flavors_order_item_pizzas_order_item_id",
                        column: x => x.order_item_id,
                        principalSchema: "ordering",
                        principalTable: "order_item_pizzas",
                        principalColumn: "order_item_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_item_pizza_flavors_pizza_flavors_pizza_flavor_id",
                        column: x => x.pizza_flavor_id,
                        principalSchema: "catalog",
                        principalTable: "pizza_flavors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_employee_id",
                schema: "audit",
                table: "audit_logs",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_unit_id_occurred_at",
                schema: "audit",
                table: "audit_logs",
                columns: new[] { "unit_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_bill_items_bill_id",
                schema: "billing",
                table: "bill_items",
                column: "bill_id");

            migrationBuilder.CreateIndex(
                name: "ix_bill_items_order_item_id",
                schema: "billing",
                table: "bill_items",
                column: "order_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_bill_split_items_bill_item_id",
                schema: "billing",
                table: "bill_split_items",
                column: "bill_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_bill_splits_bill_id_split_number",
                schema: "billing",
                table: "bill_splits",
                columns: new[] { "bill_id", "split_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bills_table_session_id_status",
                schema: "billing",
                table: "bills",
                columns: new[] { "table_session_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_bills_unit_id",
                schema: "billing",
                table: "bills",
                column: "unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_cash_movements_authorized_by_employee_id",
                schema: "cashier",
                table: "cash_movements",
                column: "authorized_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_cash_movements_cash_shift_id",
                schema: "cashier",
                table: "cash_movements",
                column: "cash_shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_cash_movements_created_by_employee_id",
                schema: "cashier",
                table: "cash_movements",
                column: "created_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_cash_movements_payment_id",
                schema: "cashier",
                table: "cash_movements",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_cash_registers_unit_id_code",
                schema: "cashier",
                table: "cash_registers",
                columns: new[] { "unit_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cash_shifts_cash_register_id_status",
                schema: "cashier",
                table: "cash_shifts",
                columns: new[] { "cash_register_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_cash_shifts_closed_by_employee_id",
                schema: "cashier",
                table: "cash_shifts",
                column: "closed_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_cash_shifts_operator_employee_id",
                schema: "cashier",
                table: "cash_shifts",
                column: "operator_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_categories_parent_category_id",
                schema: "catalog",
                table: "categories",
                column: "parent_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_categories_unit_id_slug",
                schema: "catalog",
                table: "categories",
                columns: new[] { "unit_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_device_sessions_device_id",
                schema: "devices",
                table: "device_sessions",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "ix_device_sessions_table_session_id",
                schema: "devices",
                table: "device_sessions",
                column: "table_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_devices_linked_table_id",
                schema: "devices",
                table: "devices",
                column: "linked_table_id");

            migrationBuilder.CreateIndex(
                name: "ix_devices_serial_number",
                schema: "devices",
                table: "devices",
                column: "serial_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_devices_unit_id_status",
                schema: "devices",
                table: "devices",
                columns: new[] { "unit_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_dining_areas_unit_id_name",
                schema: "dining",
                table: "dining_areas",
                columns: new[] { "unit_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_employees_unit_id_employee_code",
                schema: "identity",
                table: "employees",
                columns: new[] { "unit_id", "employee_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ingredients_inventory_item_id",
                schema: "catalog",
                table: "ingredients",
                column: "inventory_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_ingredients_unit_id",
                schema: "catalog",
                table: "ingredients",
                column: "unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_items_unit_id_sku",
                schema: "inventory",
                table: "inventory_items",
                columns: new[] { "unit_id", "sku" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_kitchen_ticket_items_kitchen_ticket_id",
                schema: "production",
                table: "kitchen_ticket_items",
                column: "kitchen_ticket_id");

            migrationBuilder.CreateIndex(
                name: "ix_kitchen_ticket_items_order_item_id",
                schema: "production",
                table: "kitchen_ticket_items",
                column: "order_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_kitchen_tickets_order_id",
                schema: "production",
                table: "kitchen_tickets",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_kitchen_tickets_production_station_id_status",
                schema: "production",
                table: "kitchen_tickets",
                columns: new[] { "production_station_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_kitchen_tickets_unit_id_ticket_number",
                schema: "production",
                table: "kitchen_tickets",
                columns: new[] { "unit_id", "ticket_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notifications_unit_id_read_at_created_at",
                schema: "notifications",
                table: "notifications",
                columns: new[] { "unit_id", "read_at", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_order_item_modifiers_ingredient_id",
                schema: "ordering",
                table: "order_item_modifiers",
                column: "ingredient_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_item_modifiers_order_item_id",
                schema: "ordering",
                table: "order_item_modifiers",
                column: "order_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_item_modifiers_pizza_flavor_id",
                schema: "ordering",
                table: "order_item_modifiers",
                column: "pizza_flavor_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_item_pizza_flavors_order_item_id",
                schema: "ordering",
                table: "order_item_pizza_flavors",
                column: "order_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_item_pizza_flavors_pizza_flavor_id",
                schema: "ordering",
                table: "order_item_pizza_flavors",
                column: "pizza_flavor_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_item_pizzas_pizza_crust_id",
                schema: "ordering",
                table: "order_item_pizzas",
                column: "pizza_crust_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_item_pizzas_pizza_size_id",
                schema: "ordering",
                table: "order_item_pizzas",
                column: "pizza_size_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_items_order_id_status",
                schema: "ordering",
                table: "order_items",
                columns: new[] { "order_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_order_items_product_id",
                schema: "ordering",
                table: "order_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_items_product_variant_id",
                schema: "ordering",
                table: "order_items",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_items_production_station_id",
                schema: "ordering",
                table: "order_items",
                column: "production_station_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_created_by_device_id",
                schema: "ordering",
                table: "orders",
                column: "created_by_device_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_created_by_employee_id",
                schema: "ordering",
                table: "orders",
                column: "created_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_table_session_id",
                schema: "ordering",
                table: "orders",
                column: "table_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_unit_id_order_number",
                schema: "ordering",
                table: "orders",
                columns: new[] { "unit_id", "order_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_orders_unit_id_status_placed_at",
                schema: "ordering",
                table: "orders",
                columns: new[] { "unit_id", "status", "placed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_methods_unit_id_code",
                schema: "billing",
                table: "payment_methods",
                columns: new[] { "unit_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payments_bill_id_status",
                schema: "billing",
                table: "payments",
                columns: new[] { "bill_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_payments_bill_split_id",
                schema: "billing",
                table: "payments",
                column: "bill_split_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_cash_shift_id",
                schema: "billing",
                table: "payments",
                column: "cash_shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_payment_method_id",
                schema: "billing",
                table: "payments",
                column: "payment_method_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_received_by_employee_id",
                schema: "billing",
                table: "payments",
                column: "received_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_payments_unit_id",
                schema: "billing",
                table: "payments",
                column: "unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_pizza_crust_prices_pizza_crust_id_pizza_size_id",
                schema: "catalog",
                table: "pizza_crust_prices",
                columns: new[] { "pizza_crust_id", "pizza_size_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pizza_crust_prices_pizza_size_id",
                schema: "catalog",
                table: "pizza_crust_prices",
                column: "pizza_size_id");

            migrationBuilder.CreateIndex(
                name: "ix_pizza_crusts_unit_id",
                schema: "catalog",
                table: "pizza_crusts",
                column: "unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_pizza_flavor_ingredients_ingredient_id",
                schema: "catalog",
                table: "pizza_flavor_ingredients",
                column: "ingredient_id");

            migrationBuilder.CreateIndex(
                name: "ix_pizza_flavor_prices_pizza_flavor_id_pizza_size_id",
                schema: "catalog",
                table: "pizza_flavor_prices",
                columns: new[] { "pizza_flavor_id", "pizza_size_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pizza_flavor_prices_pizza_size_id",
                schema: "catalog",
                table: "pizza_flavor_prices",
                column: "pizza_size_id");

            migrationBuilder.CreateIndex(
                name: "ix_pizza_flavors_category_id",
                schema: "catalog",
                table: "pizza_flavors",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_pizza_flavors_unit_id_name",
                schema: "catalog",
                table: "pizza_flavors",
                columns: new[] { "unit_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pizza_sizes_unit_id_name",
                schema: "catalog",
                table: "pizza_sizes",
                columns: new[] { "unit_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_images_product_id",
                schema: "catalog",
                table: "product_images",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_variants_product_id",
                schema: "catalog",
                table: "product_variants",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_variants_sku",
                schema: "catalog",
                table: "product_variants",
                column: "sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_production_stations_unit_id_code",
                schema: "production",
                table: "production_stations",
                columns: new[] { "unit_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_products_category_id",
                schema: "catalog",
                table: "products",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_unit_id_category_id_is_active",
                schema: "catalog",
                table: "products",
                columns: new[] { "unit_id", "category_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_products_unit_id_sku",
                schema: "catalog",
                table: "products",
                columns: new[] { "unit_id", "sku" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_recipe_items_inventory_item_id",
                schema: "inventory",
                table: "recipe_items",
                column: "inventory_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_items_recipe_id",
                schema: "inventory",
                table: "recipe_items",
                column: "recipe_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipes_pizza_flavor_id",
                schema: "inventory",
                table: "recipes",
                column: "pizza_flavor_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipes_pizza_size_id",
                schema: "inventory",
                table: "recipes",
                column: "pizza_size_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipes_product_id",
                schema: "inventory",
                table: "recipes",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipes_product_variant_id",
                schema: "inventory",
                table: "recipes",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "ix_restaurant_tables_dining_area_id",
                schema: "dining",
                table: "restaurant_tables",
                column: "dining_area_id");

            migrationBuilder.CreateIndex(
                name: "ix_restaurant_tables_unit_id_number",
                schema: "dining",
                table: "restaurant_tables",
                columns: new[] { "unit_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_restaurant_units_cnpj",
                schema: "core",
                table: "restaurant_units",
                column: "cnpj",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_claims_role_id",
                schema: "identity",
                table: "role_claims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "identity",
                table: "roles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_call_types_code",
                schema: "dining",
                table: "service_call_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_calls_assigned_employee_id",
                schema: "dining",
                table: "service_calls",
                column: "assigned_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_calls_requested_by_device_id",
                schema: "dining",
                table: "service_calls",
                column: "requested_by_device_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_calls_service_call_type_id",
                schema: "dining",
                table: "service_calls",
                column: "service_call_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_calls_table_session_id",
                schema: "dining",
                table: "service_calls",
                column: "table_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_calls_unit_id_status_created_at",
                schema: "dining",
                table: "service_calls",
                columns: new[] { "unit_id", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_balances_inventory_item_id",
                schema: "inventory",
                table: "stock_balances",
                column: "inventory_item_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_created_by_employee_id",
                schema: "inventory",
                table: "stock_movements",
                column: "created_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_inventory_item_id_created_at",
                schema: "inventory",
                table: "stock_movements",
                columns: new[] { "inventory_item_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_order_item_id",
                schema: "inventory",
                table: "stock_movements",
                column: "order_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_table_session_tables_linked_by_employee_id",
                schema: "dining",
                table: "table_session_tables",
                column: "linked_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_table_session_tables_restaurant_table_id_unlinked_at",
                schema: "dining",
                table: "table_session_tables",
                columns: new[] { "restaurant_table_id", "unlinked_at" });

            migrationBuilder.CreateIndex(
                name: "ix_table_sessions_closed_by_employee_id",
                schema: "dining",
                table: "table_sessions",
                column: "closed_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_table_sessions_opened_by_employee_id",
                schema: "dining",
                table: "table_sessions",
                column: "opened_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_table_sessions_primary_waiter_id",
                schema: "dining",
                table: "table_sessions",
                column: "primary_waiter_id");

            migrationBuilder.CreateIndex(
                name: "ix_table_sessions_unit_id_session_number",
                schema: "dining",
                table: "table_sessions",
                columns: new[] { "unit_id", "session_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_table_sessions_unit_id_status",
                schema: "dining",
                table: "table_sessions",
                columns: new[] { "unit_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_user_claims_user_id",
                schema: "identity",
                table: "user_claims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_logins_user_id",
                schema: "identity",
                table: "user_logins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role_id",
                schema: "identity",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "identity",
                table: "users",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "identity",
                table: "users",
                column: "normalized_user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_waiter_assignments_assigned_by_employee_id",
                schema: "dining",
                table: "waiter_assignments",
                column: "assigned_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_waiter_assignments_employee_id",
                schema: "dining",
                table: "waiter_assignments",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_waiter_assignments_table_session_id",
                schema: "dining",
                table: "waiter_assignments",
                column: "table_session_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "bill_split_items",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "cash_movements",
                schema: "cashier");

            migrationBuilder.DropTable(
                name: "device_sessions",
                schema: "devices");

            migrationBuilder.DropTable(
                name: "kitchen_ticket_items",
                schema: "production");

            migrationBuilder.DropTable(
                name: "notifications",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "operation_settings",
                schema: "core");

            migrationBuilder.DropTable(
                name: "order_item_modifiers",
                schema: "ordering");

            migrationBuilder.DropTable(
                name: "order_item_pizza_flavors",
                schema: "ordering");

            migrationBuilder.DropTable(
                name: "pizza_crust_prices",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "pizza_flavor_ingredients",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "pizza_flavor_prices",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "pizza_settings",
                schema: "core");

            migrationBuilder.DropTable(
                name: "product_images",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "recipe_items",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "role_claims",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "service_calls",
                schema: "dining");

            migrationBuilder.DropTable(
                name: "stock_balances",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "stock_movements",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "table_session_tables",
                schema: "dining");

            migrationBuilder.DropTable(
                name: "user_claims",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "user_logins",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "user_roles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "user_tokens",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "waiter_assignments",
                schema: "dining");

            migrationBuilder.DropTable(
                name: "bill_items",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "payments",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "kitchen_tickets",
                schema: "production");

            migrationBuilder.DropTable(
                name: "order_item_pizzas",
                schema: "ordering");

            migrationBuilder.DropTable(
                name: "ingredients",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "recipes",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "service_call_types",
                schema: "dining");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "users",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "bill_splits",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "cash_shifts",
                schema: "cashier");

            migrationBuilder.DropTable(
                name: "payment_methods",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "order_items",
                schema: "ordering");

            migrationBuilder.DropTable(
                name: "pizza_crusts",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "inventory_items",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "pizza_flavors",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "pizza_sizes",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "bills",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "cash_registers",
                schema: "cashier");

            migrationBuilder.DropTable(
                name: "orders",
                schema: "ordering");

            migrationBuilder.DropTable(
                name: "product_variants",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "production_stations",
                schema: "production");

            migrationBuilder.DropTable(
                name: "devices",
                schema: "devices");

            migrationBuilder.DropTable(
                name: "table_sessions",
                schema: "dining");

            migrationBuilder.DropTable(
                name: "products",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "restaurant_tables",
                schema: "dining");

            migrationBuilder.DropTable(
                name: "employees",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "categories",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "dining_areas",
                schema: "dining");

            migrationBuilder.DropTable(
                name: "restaurant_units",
                schema: "core");
        }
    }
}
