using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentModuleV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ✅ STEP 1: Rename old "payments" table to "contract_payments" to avoid conflict
            migrationBuilder.RenameTable(
                name: "payments",
                newName: "contract_payments");
            
            migrationBuilder.RenameIndex(
                name: "IX_payments_ContractId",
                table: "contract_payments",
                newName: "IX_contract_payments_ContractId");
            
            // ✅ STEP 2: Now we can drop columns and create new "Payments" table
            migrationBuilder.DropForeignKey(
                name: "FK_payments_contracts_ContractId",
                table: "contract_payments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_payments",
                table: "contract_payments");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "contract_payments");

            // ✅ Modify old contract_payments table
            migrationBuilder.AddPrimaryKey(
                name: "PK_contract_payments",
                table: "contract_payments",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_contract_payments_contracts_ContractId",
                table: "contract_payments",
                column: "ContractId",
                principalTable: "contracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // ✅ STEP 3: Create new Payments table (PaymentAggregate)
            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PropertyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContractId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AmountDue = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExpectedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PaymentMethod = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Month = table.Column<int>(type: "INTEGER", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    ReceiptId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                });

            // ✅ contract_payments table already exists from rename, no need to create it again

            migrationBuilder.CreateTable(
                name: "RentInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContractId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PropertyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Month = table.Column<int>(type: "INTEGER", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PaymentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentInvoices", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ContractId_Month_Year",
                table: "Payments",
                columns: new[] { "ContractId", "Month", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PropertyId",
                table: "Payments",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status",
                table: "Payments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId",
                table: "Payments",
                column: "TenantId");

            // Index IX_contract_payments_ContractId already exists from rename

            migrationBuilder.CreateIndex(
                name: "IX_RentInvoices_ContractId",
                table: "RentInvoices",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_RentInvoices_ContractId_Month_Year",
                table: "RentInvoices",
                columns: new[] { "ContractId", "Month", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RentInvoices_DueDate",
                table: "RentInvoices",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_RentInvoices_PropertyId",
                table: "RentInvoices",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_RentInvoices_Status",
                table: "RentInvoices",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RentInvoices_TenantId",
                table: "RentInvoices",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contract_payments");

            migrationBuilder.DropTable(
                name: "RentInvoices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Payments",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ContractId_Month_Year",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PropertyId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_Status",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_TenantId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "AmountDue",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "AmountPaid",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ExpectedDate",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Month",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PropertyId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ReceiptId",
                table: "Payments");

            migrationBuilder.RenameTable(
                name: "Payments",
                newName: "payments");

            migrationBuilder.RenameColumn(
                name: "Year",
                table: "payments",
                newName: "Method");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "payments",
                newName: "Code");

            migrationBuilder.RenameIndex(
                name: "IX_Payments_ContractId",
                table: "payments",
                newName: "IX_payments_ContractId");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "payments",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<DateTime>(
                name: "PaymentDate",
                table: "payments",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "payments",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddPrimaryKey(
                name: "PK_payments",
                table: "payments",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_payments_contracts_ContractId",
                table: "payments",
                column: "ContractId",
                principalTable: "contracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
