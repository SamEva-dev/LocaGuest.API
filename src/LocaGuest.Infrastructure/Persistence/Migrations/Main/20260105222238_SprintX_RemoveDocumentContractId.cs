using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Persistence.Migrations.Main
{
    /// <inheritdoc />
    public partial class SprintX_RemoveDocumentContractId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"INSERT INTO lease.contract_documents (""OrganizationId"", ""ContractId"", ""DocumentId"", ""Type"", ""LinkedAtUtc"")
SELECT d.""OrganizationId"", d.""ContractId"", d.""Id"", d.""Type"", NOW()
FROM doc.documents d
WHERE d.""ContractId"" IS NOT NULL
  AND NOT EXISTS (
      SELECT 1
      FROM lease.contract_documents cd
      WHERE cd.""OrganizationId"" = d.""OrganizationId""
        AND cd.""ContractId"" = d.""ContractId""
        AND cd.""DocumentId"" = d.""Id""
  );");

            migrationBuilder.AddForeignKey(
                name: "FK_contract_documents_contracts_ContractId",
                schema: "lease",
                table: "contract_documents",
                column: "ContractId",
                principalSchema: "lease",
                principalTable: "contracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_contract_documents_documents_DocumentId",
                schema: "lease",
                table: "contract_documents",
                column: "DocumentId",
                principalSchema: "doc",
                principalTable: "documents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropColumn(
                name: "ContractId",
                schema: "doc",
                table: "documents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ContractId",
                schema: "doc",
                table: "documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.DropForeignKey(
                name: "FK_contract_documents_contracts_ContractId",
                schema: "lease",
                table: "contract_documents");

            migrationBuilder.DropForeignKey(
                name: "FK_contract_documents_documents_DocumentId",
                schema: "lease",
                table: "contract_documents");
        }
    }
}
