using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyRentabilityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Insurance",
                table: "properties",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaintenanceRate",
                table: "properties",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ManagementFeesRate",
                table: "properties",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NightsBookedPerMonth",
                table: "properties",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PurchasePrice",
                table: "properties",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VacancyRate",
                table: "properties",
                type: "numeric(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Insurance",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "MaintenanceRate",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "ManagementFeesRate",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "NightsBookedPerMonth",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "PurchasePrice",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "VacancyRate",
                table: "properties");
        }
    }
}
