using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantDetailedInformation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "tenants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "tenants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "tenants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "tenants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContact",
                table: "tenants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyPhone",
                table: "tenants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdNumber",
                table: "tenants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyIncome",
                table: "tenants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nationality",
                table: "tenants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Occupation",
                table: "tenants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "tenants",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "City",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "EmergencyContact",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "EmergencyPhone",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "IdNumber",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "MonthlyIncome",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Nationality",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Occupation",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "tenants");
        }
    }
}
