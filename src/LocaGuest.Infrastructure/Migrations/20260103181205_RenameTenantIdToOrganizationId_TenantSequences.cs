using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameTenantIdToOrganizationId_TenantSequences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "tenant_sequences",
                newName: "OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_tenant_sequences_TenantId",
                table: "tenant_sequences",
                newName: "IX_tenant_sequences_OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "tenant_sequences",
                newName: "TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_tenant_sequences_OrganizationId",
                table: "tenant_sequences",
                newName: "IX_tenant_sequences_TenantId");
        }
    }
}
