using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class payementInvoiceInventori : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_ContractId_Month_Year",
                table: "Payments");

            migrationBuilder.AddColumn<Guid>(
                name: "InvoiceDocumentId",
                table: "Payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentType",
                table: "Payments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "FinalizedAt",
                table: "inventory_exits",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFinalized",
                table: "inventory_exits",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ContractId_Month_Year_PaymentType",
                table: "Payments",
                columns: new[] { "ContractId", "Month", "Year", "PaymentType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_ContractId_Month_Year_PaymentType",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "InvoiceDocumentId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PaymentType",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "FinalizedAt",
                table: "inventory_exits");

            migrationBuilder.DropColumn(
                name: "IsFinalized",
                table: "inventory_exits");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ContractId_Month_Year",
                table: "Payments",
                columns: new[] { "ContractId", "Month", "Year" },
                unique: true);
        }
    }
}
