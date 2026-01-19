using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Persistence.Migrations.Main
{
    /// <inheritdoc />
    public partial class AddSatisfactionFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "satisfaction_feedback",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_satisfaction_feedback", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_satisfaction_feedback_CreatedAtUtc",
                schema: "analytics",
                table: "satisfaction_feedback",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_satisfaction_feedback_OrganizationId",
                schema: "analytics",
                table: "satisfaction_feedback",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_satisfaction_feedback_UserId",
                schema: "analytics",
                table: "satisfaction_feedback",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "satisfaction_feedback",
                schema: "analytics");
        }
    }
}
