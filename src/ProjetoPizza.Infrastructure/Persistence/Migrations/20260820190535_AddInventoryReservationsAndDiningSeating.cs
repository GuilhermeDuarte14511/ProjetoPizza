using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoPizza.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryReservationsAndDiningSeating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "seated_at",
                schema: "dining",
                table: "waitlist_entries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "table_session_id",
                schema: "dining",
                table: "waitlist_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "seated_at",
                schema: "dining",
                table: "reservations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "table_session_id",
                schema: "dining",
                table: "reservations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "inventory_reservations",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inventory_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    reserved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    released_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_reservations", x => x.id);
                    table.ForeignKey(
                        name: "fk_inventory_reservations_inventory_items_inventory_item_id",
                        column: x => x.inventory_item_id,
                        principalSchema: "inventory",
                        principalTable: "inventory_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inventory_reservations_order_items_order_item_id",
                        column: x => x.order_item_id,
                        principalSchema: "ordering",
                        principalTable: "order_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_waitlist_entries_table_session_id",
                schema: "dining",
                table: "waitlist_entries",
                column: "table_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_reservations_table_session_id",
                schema: "dining",
                table: "reservations",
                column: "table_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_reservations_inventory_item_id_status",
                schema: "inventory",
                table: "inventory_reservations",
                columns: new[] { "inventory_item_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_reservations_order_item_id_inventory_item_id",
                schema: "inventory",
                table: "inventory_reservations",
                columns: new[] { "order_item_id", "inventory_item_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_reservations_table_sessions_table_session_id",
                schema: "dining",
                table: "reservations",
                column: "table_session_id",
                principalSchema: "dining",
                principalTable: "table_sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_waitlist_entries_table_sessions_table_session_id",
                schema: "dining",
                table: "waitlist_entries",
                column: "table_session_id",
                principalSchema: "dining",
                principalTable: "table_sessions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_reservations_table_sessions_table_session_id",
                schema: "dining",
                table: "reservations");

            migrationBuilder.DropForeignKey(
                name: "fk_waitlist_entries_table_sessions_table_session_id",
                schema: "dining",
                table: "waitlist_entries");

            migrationBuilder.DropTable(
                name: "inventory_reservations",
                schema: "inventory");

            migrationBuilder.DropIndex(
                name: "ix_waitlist_entries_table_session_id",
                schema: "dining",
                table: "waitlist_entries");

            migrationBuilder.DropIndex(
                name: "ix_reservations_table_session_id",
                schema: "dining",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "seated_at",
                schema: "dining",
                table: "waitlist_entries");

            migrationBuilder.DropColumn(
                name: "table_session_id",
                schema: "dining",
                table: "waitlist_entries");

            migrationBuilder.DropColumn(
                name: "seated_at",
                schema: "dining",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "table_session_id",
                schema: "dining",
                table: "reservations");
        }
    }
}
