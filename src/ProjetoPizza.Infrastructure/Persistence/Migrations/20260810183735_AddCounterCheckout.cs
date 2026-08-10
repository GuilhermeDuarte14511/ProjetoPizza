using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoPizza.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCounterCheckout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "table_session_id",
                schema: "billing",
                table: "bills",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "order_id",
                schema: "billing",
                table: "bills",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_bills_order_id",
                schema: "billing",
                table: "bills",
                column: "order_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_bills_orders_order_id",
                schema: "billing",
                table: "bills",
                column: "order_id",
                principalSchema: "ordering",
                principalTable: "orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_bills_orders_order_id",
                schema: "billing",
                table: "bills");

            migrationBuilder.DropIndex(
                name: "ix_bills_order_id",
                schema: "billing",
                table: "bills");

            migrationBuilder.DropColumn(
                name: "order_id",
                schema: "billing",
                table: "bills");

            migrationBuilder.AlterColumn<Guid>(
                name: "table_session_id",
                schema: "billing",
                table: "bills",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
