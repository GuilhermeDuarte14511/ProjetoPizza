using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoPizza.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClientTabletSessionExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_device_sessions_device_id_ended_at",
                schema: "devices",
                table: "device_sessions");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expires_at",
                schema: "devices",
                table: "device_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE devices.device_sessions
                SET expires_at = started_at + INTERVAL '12 hours'
                WHERE expires_at IS NULL;
                """);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "expires_at",
                schema: "devices",
                table: "device_sessions",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_device_sessions_device_id_ended_at_expires_at",
                schema: "devices",
                table: "device_sessions",
                columns: new[] { "device_id", "ended_at", "expires_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_device_sessions_device_id_ended_at_expires_at",
                schema: "devices",
                table: "device_sessions");

            migrationBuilder.DropColumn(
                name: "expires_at",
                schema: "devices",
                table: "device_sessions");

            migrationBuilder.CreateIndex(
                name: "ix_device_sessions_device_id_ended_at",
                schema: "devices",
                table: "device_sessions",
                columns: new[] { "device_id", "ended_at" });
        }
    }
}
