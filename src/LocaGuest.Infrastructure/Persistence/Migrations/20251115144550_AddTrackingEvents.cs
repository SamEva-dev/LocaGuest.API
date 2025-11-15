using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackingEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tracking_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PageName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    SessionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DurationMs = table.Column<int>(type: "integer", nullable: true),
                    HttpStatusCode = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tracking_events", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tracking_events_EventType",
                table: "tracking_events",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_tracking_events_EventType_Timestamp",
                table: "tracking_events",
                columns: new[] { "EventType", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_tracking_events_TenantId",
                table: "tracking_events",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_tracking_events_TenantId_Timestamp",
                table: "tracking_events",
                columns: new[] { "TenantId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_tracking_events_TenantId_UserId_Timestamp",
                table: "tracking_events",
                columns: new[] { "TenantId", "UserId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_tracking_events_Timestamp",
                table: "tracking_events",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_tracking_events_UserId",
                table: "tracking_events",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tracking_events");
        }
    }
}
