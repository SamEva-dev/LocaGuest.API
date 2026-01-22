using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Persistence.Migrations.Main
{
    /// <inheritdoc />
    public partial class AddEmailDeliveryEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "messaging");

            migrationBuilder.CreateTable(
                name: "email_delivery_events",
                schema: "messaging",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TsEvent = table.Column<long>(type: "bigint", nullable: false),
                    RawPayload = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_delivery_events", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_email_delivery_events_CreatedAtUtc",
                schema: "messaging",
                table: "email_delivery_events",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_email_delivery_events_MessageId_EventType_TsEvent",
                schema: "messaging",
                table: "email_delivery_events",
                columns: new[] { "MessageId", "EventType", "TsEvent" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_delivery_events",
                schema: "messaging");
        }
    }
}
