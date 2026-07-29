using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoPizza.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClientTabletSessionIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_device_sessions_device_id",
                schema: "devices",
                table: "device_sessions");

            migrationBuilder.CreateIndex(
                name: "ix_device_sessions_device_id_ended_at",
                schema: "devices",
                table: "device_sessions",
                columns: new[] { "device_id", "ended_at" });

            migrationBuilder.CreateIndex(
                name: "ix_device_sessions_session_token_hash",
                schema: "devices",
                table: "device_sessions",
                column: "session_token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_device_sessions_device_id_ended_at",
                schema: "devices",
                table: "device_sessions");

            migrationBuilder.DropIndex(
                name: "ix_device_sessions_session_token_hash",
                schema: "devices",
                table: "device_sessions");

            migrationBuilder.CreateIndex(
                name: "ix_device_sessions_device_id",
                schema: "devices",
                table: "device_sessions",
                column: "device_id");
        }
    }
}
