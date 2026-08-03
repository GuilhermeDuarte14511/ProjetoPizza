using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoPizza.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PersistTabletAccessAndSelfService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "opened_by_employee_id",
                schema: "dining",
                table: "table_sessions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "opened_by_device_id",
                schema: "dining",
                table: "table_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "linked_by_employee_id",
                schema: "dining",
                table: "table_session_tables",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "linked_by_device_id",
                schema: "dining",
                table: "table_session_tables",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "table_session_id",
                schema: "devices",
                table: "device_sessions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "expires_at",
                schema: "devices",
                table: "device_sessions",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.Sql(
                "UPDATE devices.device_sessions SET expires_at = NULL WHERE ended_at IS NULL;");

            migrationBuilder.CreateIndex(
                name: "ix_table_sessions_opened_by_device_id",
                schema: "dining",
                table: "table_sessions",
                column: "opened_by_device_id");

            migrationBuilder.CreateIndex(
                name: "ix_table_session_tables_linked_by_device_id",
                schema: "dining",
                table: "table_session_tables",
                column: "linked_by_device_id");

            migrationBuilder.AddForeignKey(
                name: "fk_table_session_tables_devices_linked_by_device_id",
                schema: "dining",
                table: "table_session_tables",
                column: "linked_by_device_id",
                principalSchema: "devices",
                principalTable: "devices",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_table_sessions_devices_opened_by_device_id",
                schema: "dining",
                table: "table_sessions",
                column: "opened_by_device_id",
                principalSchema: "devices",
                principalTable: "devices",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM dining.table_sessions WHERE opened_by_device_id IS NOT NULL)
                       OR EXISTS (SELECT 1 FROM dining.table_session_tables WHERE linked_by_device_id IS NOT NULL)
                       OR EXISTS (SELECT 1 FROM devices.device_sessions WHERE table_session_id IS NULL) THEN
                        RAISE EXCEPTION 'Cannot downgrade after persistent tablet access or tablet-opened sessions have been used.';
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql(
                "UPDATE devices.device_sessions SET expires_at = started_at + INTERVAL '12 hours' WHERE expires_at IS NULL;");

            migrationBuilder.DropForeignKey(
                name: "fk_table_session_tables_devices_linked_by_device_id",
                schema: "dining",
                table: "table_session_tables");

            migrationBuilder.DropForeignKey(
                name: "fk_table_sessions_devices_opened_by_device_id",
                schema: "dining",
                table: "table_sessions");

            migrationBuilder.DropIndex(
                name: "ix_table_sessions_opened_by_device_id",
                schema: "dining",
                table: "table_sessions");

            migrationBuilder.DropIndex(
                name: "ix_table_session_tables_linked_by_device_id",
                schema: "dining",
                table: "table_session_tables");

            migrationBuilder.DropColumn(
                name: "opened_by_device_id",
                schema: "dining",
                table: "table_sessions");

            migrationBuilder.DropColumn(
                name: "linked_by_device_id",
                schema: "dining",
                table: "table_session_tables");

            migrationBuilder.AlterColumn<Guid>(
                name: "opened_by_employee_id",
                schema: "dining",
                table: "table_sessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "linked_by_employee_id",
                schema: "dining",
                table: "table_session_tables",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "table_session_id",
                schema: "devices",
                table: "device_sessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "expires_at",
                schema: "devices",
                table: "device_sessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
