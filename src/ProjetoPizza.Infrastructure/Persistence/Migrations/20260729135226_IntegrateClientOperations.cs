using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoPizza.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IntegrateClientOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "kitchen_ticket_number_sequence",
                schema: "production");

            migrationBuilder.CreateSequence(
                name: "order_number_sequence",
                schema: "ordering");

            migrationBuilder.CreateSequence(
                name: "table_session_number_sequence",
                schema: "dining");

            migrationBuilder.AddColumn<int>(
                name: "requested_split_count",
                schema: "billing",
                table: "bills",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                SELECT setval(
                    'ordering.order_number_sequence',
                    COALESCE((SELECT MAX(order_number) FROM ordering.orders), 0) + 1,
                    false);
                SELECT setval(
                    'production.kitchen_ticket_number_sequence',
                    COALESCE((SELECT MAX(ticket_number) FROM production.kitchen_tickets), 0) + 1,
                    false);
                SELECT setval(
                    'dining.table_session_number_sequence',
                    COALESCE((SELECT MAX(session_number) FROM dining.table_sessions), 0) + 1,
                    false);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "requested_split_count",
                schema: "billing",
                table: "bills");

            migrationBuilder.DropSequence(
                name: "kitchen_ticket_number_sequence",
                schema: "production");

            migrationBuilder.DropSequence(
                name: "order_number_sequence",
                schema: "ordering");

            migrationBuilder.DropSequence(
                name: "table_session_number_sequence",
                schema: "dining");
        }
    }
}
