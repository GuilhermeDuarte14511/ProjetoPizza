using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoPizza.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "refund_reason",
                schema: "billing",
                table: "payments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "refunded_amount",
                schema: "billing",
                table: "payments",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "refunded_at",
                schema: "billing",
                table: "payments",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "refund_reason",
                schema: "billing",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "refunded_amount",
                schema: "billing",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "refunded_at",
                schema: "billing",
                table: "payments");
        }
    }
}
