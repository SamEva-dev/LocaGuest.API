using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContractRenewalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CurrentIRL",
                table: "contracts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomClauses",
                table: "contracts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PreviousIRL",
                table: "contracts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RenewedContractId",
                table: "contracts",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentIRL",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "CustomClauses",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "PreviousIRL",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "RenewedContractId",
                table: "contracts");
        }
    }
}
