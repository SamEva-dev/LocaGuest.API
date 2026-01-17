using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Persistence.Migrations.Main
{
    /// <inheritdoc />
    public partial class Sprint5_IdempotencyResponseCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "response_body_base64",
                schema: "ops",
                table: "idempotency_requests",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "response_content_type",
                schema: "ops",
                table: "idempotency_requests",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "response_body_base64",
                schema: "ops",
                table: "idempotency_requests");

            migrationBuilder.DropColumn(
                name: "response_content_type",
                schema: "ops",
                table: "idempotency_requests");
        }
    }
}
