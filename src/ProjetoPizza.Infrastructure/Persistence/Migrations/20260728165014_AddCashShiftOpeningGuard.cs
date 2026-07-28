using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoPizza.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCashShiftOpeningGuard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "active_slot",
                schema: "cashier",
                table: "cash_shifts",
                type: "integer",
                nullable: true,
                computedColumnSql: "CASE WHEN status IN ('Open', 'Closing') THEN 1 ELSE NULL END",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "ix_cash_shifts_single_active",
                schema: "cashier",
                table: "cash_shifts",
                column: "active_slot",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_cash_shifts_single_active",
                schema: "cashier",
                table: "cash_shifts");

            migrationBuilder.DropColumn(
                name: "active_slot",
                schema: "cashier",
                table: "cash_shifts");
        }
    }
}
