using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyDiagnosticsAndAdminFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AcquisitionDate",
                table: "properties",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AsbestosDiagnosticDate",
                table: "properties",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CadastralReference",
                table: "properties",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CondominiumCharges",
                table: "properties",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DpeRating",
                table: "properties",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DpeValue",
                table: "properties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ElectricDiagnosticDate",
                table: "properties",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ElectricDiagnosticExpiry",
                table: "properties",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErpZone",
                table: "properties",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GasDiagnosticDate",
                table: "properties",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GasDiagnosticExpiry",
                table: "properties",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GesRating",
                table: "properties",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasAsbestos",
                table: "properties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LotNumber",
                table: "properties",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PropertyTax",
                table: "properties",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalWorksAmount",
                table: "properties",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcquisitionDate",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "AsbestosDiagnosticDate",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "CadastralReference",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "CondominiumCharges",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "DpeRating",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "DpeValue",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "ElectricDiagnosticDate",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "ElectricDiagnosticExpiry",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "ErpZone",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "GasDiagnosticDate",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "GasDiagnosticExpiry",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "GesRating",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "HasAsbestos",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "LotNumber",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "PropertyTax",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "TotalWorksAmount",
                table: "properties");
        }
    }
}
