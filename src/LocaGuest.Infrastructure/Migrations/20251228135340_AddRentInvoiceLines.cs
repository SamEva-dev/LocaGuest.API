using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRentInvoiceLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RentInvoiceLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RentInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmountDue = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ShareType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ShareValue = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaidDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentInvoiceLines", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RentInvoiceLines_RentInvoiceId",
                table: "RentInvoiceLines",
                column: "RentInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_RentInvoiceLines_RentInvoiceId_TenantId",
                table: "RentInvoiceLines",
                columns: new[] { "RentInvoiceId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RentInvoiceLines_Status",
                table: "RentInvoiceLines",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RentInvoiceLines_TenantId",
                table: "RentInvoiceLines",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RentInvoiceLines");
        }
    }
}
