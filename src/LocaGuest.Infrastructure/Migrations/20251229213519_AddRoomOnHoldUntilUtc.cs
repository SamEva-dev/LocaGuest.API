using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomOnHoldUntilUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OnHoldUntilUtc",
                table: "property_rooms",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "ContractParticipants",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "RenterTenantId",
                table: "ContractParticipants",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_property_rooms_OnHoldUntilUtc",
                table: "property_rooms",
                column: "OnHoldUntilUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_property_rooms_OnHoldUntilUtc",
                table: "property_rooms");

            migrationBuilder.DropColumn(
                name: "OnHoldUntilUtc",
                table: "property_rooms");

            migrationBuilder.DropColumn(
                name: "RenterTenantId",
                table: "ContractParticipants");

            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId",
                table: "ContractParticipants",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
