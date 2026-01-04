using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class P02_Provisioning_Idempotency_And_Sequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "core");

            migrationBuilder.CreateSequence(
                name: "organization_number_seq",
                schema: "core");

            migrationBuilder.CreateTable(
                name: "idempotency_requests",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<string>(type: "text", nullable: false),
                    idempotency_key = table.Column<string>(type: "text", nullable: false),
                    request_hash = table.Column<string>(type: "text", nullable: false),
                    response_json = table.Column<string>(type: "text", nullable: false),
                    status_code = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_requests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_idempotency_requests_client_id_idempotency_key",
                schema: "core",
                table: "idempotency_requests",
                columns: new[] { "client_id", "idempotency_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "idempotency_requests",
                schema: "core");

            migrationBuilder.DropSequence(
                name: "organization_number_seq",
                schema: "core");
        }
    }
}
