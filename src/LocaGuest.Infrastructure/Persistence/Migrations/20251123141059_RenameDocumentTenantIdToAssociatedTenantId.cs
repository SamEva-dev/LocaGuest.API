using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Migrations.LocaGuestDb
{
    /// <inheritdoc />
    public partial class RenameDocumentTenantIdToAssociatedTenantId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Supprimer l'ancien index sur TenantId (si existe)
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS IX_Documents_TenantId;
            ");
            
            // Renommer la colonne TenantId existante (Guid?) en AssociatedTenantId
            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "Documents",
                newName: "AssociatedTenantId");

            // Ajouter la nouvelle colonne TenantId (string) pour AuditableEntity
            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Documents",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
            
            // Créer le nouvel index sur AssociatedTenantId
            migrationBuilder.CreateIndex(
                name: "IX_Documents_AssociatedTenantId",
                table: "Documents",
                column: "AssociatedTenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Supprimer l'index sur AssociatedTenantId
            migrationBuilder.DropIndex(
                name: "IX_Documents_AssociatedTenantId",
                table: "Documents");

            // Supprimer la colonne TenantId (string) de AuditableEntity
            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Documents");

            // Renommer AssociatedTenantId en TenantId
            migrationBuilder.RenameColumn(
                name: "AssociatedTenantId",
                table: "Documents",
                newName: "TenantId");
            
            // Recréer l'index sur TenantId (si nécessaire)
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS IX_Documents_TenantId ON Documents(TenantId);
            ");
        }
    }
}
