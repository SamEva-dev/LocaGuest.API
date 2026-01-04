using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Persistence.Migrations.Main
{
    /// <inheritdoc />
    public partial class Sprint3_PersistenceHardening_1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_rent_invoices_ContractId_Month_Year",
                schema: "finance",
                table: "rent_invoices");

            migrationBuilder.DropIndex(
                name: "IX_payments_ContractId_Month_Year_PaymentType",
                schema: "finance",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_documents_Code",
                schema: "doc",
                table: "documents");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                schema: "locaguest",
                table: "occupants",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                schema: "lease",
                table: "contracts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_rent_invoices_OrganizationId_ContractId_Month_Year",
                schema: "finance",
                table: "rent_invoices",
                columns: new[] { "OrganizationId", "ContractId", "Month", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_OrganizationId_ContractId_Month_Year_PaymentType",
                schema: "finance",
                table: "payments",
                columns: new[] { "OrganizationId", "ContractId", "Month", "Year", "PaymentType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_occupants_OrganizationId_Code",
                schema: "locaguest",
                table: "occupants",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_occupants_OrganizationId_Email",
                schema: "locaguest",
                table: "occupants",
                columns: new[] { "OrganizationId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_documents_OrganizationId_Code",
                schema: "doc",
                table: "documents",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_contracts_OrganizationId_Code",
                schema: "lease",
                table: "contracts",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_contracts_PropertyId",
                schema: "lease",
                table: "contracts",
                column: "PropertyId");

            migrationBuilder.AddForeignKey(
                name: "FK_contracts_occupants_RenterTenantId",
                schema: "lease",
                table: "contracts",
                column: "RenterTenantId",
                principalSchema: "locaguest",
                principalTable: "occupants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_contracts_properties_PropertyId",
                schema: "lease",
                table: "contracts",
                column: "PropertyId",
                principalSchema: "locaguest",
                principalTable: "properties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_contracts_occupants_RenterTenantId",
                schema: "lease",
                table: "contracts");

            migrationBuilder.DropForeignKey(
                name: "FK_contracts_properties_PropertyId",
                schema: "lease",
                table: "contracts");

            migrationBuilder.DropIndex(
                name: "IX_rent_invoices_OrganizationId_ContractId_Month_Year",
                schema: "finance",
                table: "rent_invoices");

            migrationBuilder.DropIndex(
                name: "IX_payments_OrganizationId_ContractId_Month_Year_PaymentType",
                schema: "finance",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_occupants_OrganizationId_Code",
                schema: "locaguest",
                table: "occupants");

            migrationBuilder.DropIndex(
                name: "IX_occupants_OrganizationId_Email",
                schema: "locaguest",
                table: "occupants");

            migrationBuilder.DropIndex(
                name: "IX_documents_OrganizationId_Code",
                schema: "doc",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "IX_contracts_OrganizationId_Code",
                schema: "lease",
                table: "contracts");

            migrationBuilder.DropIndex(
                name: "IX_contracts_PropertyId",
                schema: "lease",
                table: "contracts");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                schema: "locaguest",
                table: "occupants",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                schema: "lease",
                table: "contracts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateIndex(
                name: "IX_rent_invoices_ContractId_Month_Year",
                schema: "finance",
                table: "rent_invoices",
                columns: new[] { "ContractId", "Month", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_ContractId_Month_Year_PaymentType",
                schema: "finance",
                table: "payments",
                columns: new[] { "ContractId", "Month", "Year", "PaymentType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_documents_Code",
                schema: "doc",
                table: "documents",
                column: "Code",
                unique: true);
        }
    }
}
