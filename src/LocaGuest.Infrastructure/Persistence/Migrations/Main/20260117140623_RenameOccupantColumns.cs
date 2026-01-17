using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Persistence.Migrations.Main
{
    /// <inheritdoc />
    public partial class RenameOccupantColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename RenterTenantId to RenterOccupantId in contracts table
            migrationBuilder.RenameColumn(
                name: "RenterTenantId",
                schema: "lease",
                table: "contracts",
                newName: "RenterOccupantId");

            // Rename RenterTenantId to RenterOccupantId in payments table
            migrationBuilder.RenameColumn(
                name: "RenterTenantId",
                schema: "finance",
                table: "payments",
                newName: "RenterOccupantId");

            // Rename RenterTenantId to RenterOccupantId in contract_participants table
            migrationBuilder.RenameColumn(
                name: "RenterTenantId",
                schema: "lease",
                table: "contract_participants",
                newName: "RenterOccupantId");

            // Rename RenterTenantId to RenterOccupantId in rent_invoices table
            migrationBuilder.RenameColumn(
                name: "RenterTenantId",
                schema: "finance",
                table: "rent_invoices",
                newName: "RenterOccupantId");

            // Rename RenterTenantId to RenterOccupantId in rent_invoice_lines table
            migrationBuilder.RenameColumn(
                name: "RenterTenantId",
                schema: "finance",
                table: "rent_invoice_lines",
                newName: "RenterOccupantId");

            // Rename RenterTenantId to RenterOccupantId in inventory_entries table
            migrationBuilder.RenameColumn(
                name: "RenterTenantId",
                schema: "inventory",
                table: "inventory_entries",
                newName: "RenterOccupantId");

            // Rename RenterTenantId to RenterOccupantId in inventory_exits table
            migrationBuilder.RenameColumn(
                name: "RenterTenantId",
                schema: "inventory",
                table: "inventory_exits",
                newName: "RenterOccupantId");

            // Rename AssociatedTenantId to AssociatedOccupantId in documents table
            migrationBuilder.RenameColumn(
                name: "AssociatedTenantId",
                schema: "doc",
                table: "documents",
                newName: "AssociatedOccupantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(name: "RenterOccupantId", schema: "lease", table: "contracts", newName: "RenterTenantId");
            migrationBuilder.RenameColumn(name: "RenterOccupantId", schema: "finance", table: "payments", newName: "RenterTenantId");
            migrationBuilder.RenameColumn(name: "RenterOccupantId", schema: "lease", table: "contract_participants", newName: "RenterTenantId");
            migrationBuilder.RenameColumn(name: "RenterOccupantId", schema: "finance", table: "rent_invoices", newName: "RenterTenantId");
            migrationBuilder.RenameColumn(name: "RenterOccupantId", schema: "finance", table: "rent_invoice_lines", newName: "RenterTenantId");
            migrationBuilder.RenameColumn(name: "RenterOccupantId", schema: "inventory", table: "inventory_entries", newName: "RenterTenantId");
            migrationBuilder.RenameColumn(name: "RenterOccupantId", schema: "inventory", table: "inventory_exits", newName: "RenterTenantId");
            migrationBuilder.RenameColumn(name: "AssociatedOccupantId", schema: "doc", table: "documents", newName: "AssociatedTenantId");
        }
    }
}
