using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Persistence.Migrations.Main
{
    /// <inheritdoc />
    public partial class SprintX_AddOrgIdToPropertyChildren : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_addendums_contracts_ContractId",
                schema: "lease",
                table: "addendums");

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                schema: "locaguest",
                table: "property_rooms",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                schema: "locaguest",
                table: "property_images",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "contract_documents",
                schema: "lease",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LinkedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contract_documents", x => new { x.OrganizationId, x.ContractId, x.DocumentId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_property_rooms_OrganizationId",
                schema: "locaguest",
                table: "property_rooms",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_property_rooms_OrganizationId_PropertyId",
                schema: "locaguest",
                table: "property_rooms",
                columns: new[] { "OrganizationId", "PropertyId" });

            migrationBuilder.CreateIndex(
                name: "IX_property_images_OrganizationId",
                schema: "locaguest",
                table: "property_images",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_property_images_OrganizationId_PropertyId",
                schema: "locaguest",
                table: "property_images",
                columns: new[] { "OrganizationId", "PropertyId" });

            migrationBuilder.CreateIndex(
                name: "IX_contract_documents_ContractId",
                schema: "lease",
                table: "contract_documents",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_contract_documents_DocumentId",
                schema: "lease",
                table: "contract_documents",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_contract_documents_OrganizationId",
                schema: "lease",
                table: "contract_documents",
                column: "OrganizationId");

            migrationBuilder.Sql(@"UPDATE locaguest.property_rooms pr
SET ""OrganizationId"" = p.""OrganizationId""
FROM locaguest.properties p
WHERE pr.""PropertyId"" = p.""Id"" AND pr.""OrganizationId"" IS NULL;");

            migrationBuilder.Sql(@"UPDATE locaguest.property_images pi
SET ""OrganizationId"" = p.""OrganizationId""
FROM locaguest.properties p
WHERE pi.""PropertyId"" = p.""Id"" AND pi.""OrganizationId"" IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrganizationId",
                schema: "locaguest",
                table: "property_rooms",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "OrganizationId",
                schema: "locaguest",
                table: "property_images",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_addendums_contracts_ContractId",
                schema: "lease",
                table: "addendums",
                column: "ContractId",
                principalSchema: "lease",
                principalTable: "contracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_addendums_contracts_ContractId",
                schema: "lease",
                table: "addendums");

            migrationBuilder.DropTable(
                name: "contract_documents",
                schema: "lease");

            migrationBuilder.DropIndex(
                name: "IX_property_rooms_OrganizationId",
                schema: "locaguest",
                table: "property_rooms");

            migrationBuilder.DropIndex(
                name: "IX_property_rooms_OrganizationId_PropertyId",
                schema: "locaguest",
                table: "property_rooms");

            migrationBuilder.DropIndex(
                name: "IX_property_images_OrganizationId",
                schema: "locaguest",
                table: "property_images");

            migrationBuilder.DropIndex(
                name: "IX_property_images_OrganizationId_PropertyId",
                schema: "locaguest",
                table: "property_images");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                schema: "locaguest",
                table: "property_rooms");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                schema: "locaguest",
                table: "property_images");

            migrationBuilder.AddForeignKey(
                name: "FK_addendums_contracts_ContractId",
                schema: "lease",
                table: "addendums",
                column: "ContractId",
                principalSchema: "lease",
                principalTable: "contracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
