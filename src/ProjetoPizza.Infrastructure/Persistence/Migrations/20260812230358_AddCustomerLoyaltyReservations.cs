using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoPizza.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerLoyaltyReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_order_at",
                schema: "customers",
                table: "customers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "lifetime_spend",
                schema: "customers",
                table: "customers",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "loyalty_points",
                schema: "customers",
                table: "customers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "order_count",
                schema: "customers",
                table: "customers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "reservations",
                schema: "dining",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    phone = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    party_size = table.Column<int>(type: "integer", nullable: false),
                    scheduled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reservations", x => x.id);
                    table.ForeignKey(
                        name: "fk_reservations_customers_customer_id",
                        column: x => x.customer_id,
                        principalSchema: "customers",
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_reservations_restaurant_units_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "core",
                        principalTable: "restaurant_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "waitlist_entries",
                schema: "dining",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    phone = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    party_size = table.Column<int>(type: "integer", nullable: false),
                    estimated_wait_minutes = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    entered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    notified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_waitlist_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_waitlist_entries_customers_customer_id",
                        column: x => x.customer_id,
                        principalSchema: "customers",
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_waitlist_entries_restaurant_units_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "core",
                        principalTable: "restaurant_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_reservations_customer_id",
                schema: "dining",
                table: "reservations",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_reservations_unit_id_scheduled_at_status",
                schema: "dining",
                table: "reservations",
                columns: new[] { "unit_id", "scheduled_at", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_waitlist_entries_customer_id",
                schema: "dining",
                table: "waitlist_entries",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_waitlist_entries_unit_id_status_entered_at",
                schema: "dining",
                table: "waitlist_entries",
                columns: new[] { "unit_id", "status", "entered_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reservations",
                schema: "dining");

            migrationBuilder.DropTable(
                name: "waitlist_entries",
                schema: "dining");

            migrationBuilder.DropColumn(
                name: "last_order_at",
                schema: "customers",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "lifetime_spend",
                schema: "customers",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "loyalty_points",
                schema: "customers",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "order_count",
                schema: "customers",
                table: "customers");
        }
    }
}
