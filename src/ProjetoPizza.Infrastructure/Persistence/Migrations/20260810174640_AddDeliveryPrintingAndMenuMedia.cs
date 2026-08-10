using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoPizza.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryPrintingAndMenuMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "delivered_at",
                schema: "ordering",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "delivery_driver_name",
                schema: "ordering",
                table: "orders",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "delivery_failure_reason",
                schema: "ordering",
                table: "orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "delivery_status",
                schema: "ordering",
                table: "orders",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "delivery_tracking_token_hash",
                schema: "ordering",
                table: "orders",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "dispatched_at",
                schema: "ordering",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE ordering.orders
                SET delivery_status = CASE
                    WHEN status = 'Ready' THEN 'ReadyForDispatch'
                    WHEN status = 'Completed' THEN 'Delivered'
                    WHEN status = 'Cancelled' THEN 'Cancelled'
                    ELSE 'AwaitingPreparation'
                END
                WHERE fulfillment_type = 'Delivery' AND delivery_status IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ip_address",
                schema: "devices",
                table: "devices",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "auto_print_customer_receipts",
                schema: "devices",
                table: "devices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "auto_print_fiscal_documents",
                schema: "devices",
                table: "devices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "auto_print_kitchen_tickets",
                schema: "devices",
                table: "devices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "paper_width_mm",
                schema: "devices",
                table: "devices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "printer_port",
                schema: "devices",
                table: "devices",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "print_jobs",
                schema: "devices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    printer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    payload = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                    copies = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_print_jobs", x => x.id);
                    table.ForeignKey(
                        name: "fk_print_jobs_devices_printer_id",
                        column: x => x.printer_id,
                        principalSchema: "devices",
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_print_jobs_restaurant_units_unit_id",
                        column: x => x.unit_id,
                        principalSchema: "core",
                        principalTable: "restaurant_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_orders_delivery_tracking_token_hash",
                schema: "ordering",
                table: "orders",
                column: "delivery_tracking_token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_print_jobs_printer_id",
                schema: "devices",
                table: "print_jobs",
                column: "printer_id");

            migrationBuilder.CreateIndex(
                name: "ix_print_jobs_status_next_attempt_at",
                schema: "devices",
                table: "print_jobs",
                columns: new[] { "status", "next_attempt_at" });

            migrationBuilder.CreateIndex(
                name: "ix_print_jobs_unit_id",
                schema: "devices",
                table: "print_jobs",
                column: "unit_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "print_jobs",
                schema: "devices");

            migrationBuilder.DropIndex(
                name: "ix_orders_delivery_tracking_token_hash",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "delivered_at",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "delivery_driver_name",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "delivery_failure_reason",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "delivery_status",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "delivery_tracking_token_hash",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "dispatched_at",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "auto_print_customer_receipts",
                schema: "devices",
                table: "devices");

            migrationBuilder.DropColumn(
                name: "auto_print_fiscal_documents",
                schema: "devices",
                table: "devices");

            migrationBuilder.DropColumn(
                name: "auto_print_kitchen_tickets",
                schema: "devices",
                table: "devices");

            migrationBuilder.DropColumn(
                name: "paper_width_mm",
                schema: "devices",
                table: "devices");

            migrationBuilder.DropColumn(
                name: "printer_port",
                schema: "devices",
                table: "devices");

            migrationBuilder.AlterColumn<string>(
                name: "ip_address",
                schema: "devices",
                table: "devices",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);
        }
    }
}
