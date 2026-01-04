using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class P03_Invitations_Tier1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "invitations",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SecretHash = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcceptedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invitations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_invitations_ExpiresAtUtc",
                schema: "core",
                table: "invitations",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_invitations_OrganizationId_Email_Status",
                schema: "core",
                table: "invitations",
                columns: new[] { "OrganizationId", "Email", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "invitations",
                schema: "core");
        }
    }
}
