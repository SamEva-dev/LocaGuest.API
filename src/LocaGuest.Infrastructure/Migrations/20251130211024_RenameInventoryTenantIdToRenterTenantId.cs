using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameInventoryTenantIdToRenterTenantId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RenterTenantId",
                table: "inventory_exits",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "RenterTenantId",
                table: "inventory_entries",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_inventory_exits_RenterTenantId",
                table: "inventory_exits",
                column: "RenterTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_entries_RenterTenantId",
                table: "inventory_entries",
                column: "RenterTenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_inventory_exits_RenterTenantId",
                table: "inventory_exits");

            migrationBuilder.DropIndex(
                name: "IX_inventory_entries_RenterTenantId",
                table: "inventory_entries");

            migrationBuilder.DropColumn(
                name: "RenterTenantId",
                table: "inventory_exits");

            migrationBuilder.DropColumn(
                name: "RenterTenantId",
                table: "inventory_entries");
        }
    }
}
