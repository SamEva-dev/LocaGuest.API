using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using LocaGuest.Infrastructure.Persistence;

#nullable disable

namespace LocaGuest.Infrastructure.Migrations
{
    [DbContext(typeof(LocaGuestDbContext))]
    [Migration("20251229011000_AddInvoiceDocumentIdToRentInvoices")]
    public partial class AddInvoiceDocumentIdToRentInvoices : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "InvoiceDocumentId",
                table: "RentInvoices",
                type: "uuid",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvoiceDocumentId",
                table: "RentInvoices");
        }
    }
}
