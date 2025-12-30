using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RestorePropertyFieldsAndRenameDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "properties",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "AcquisitionDate",
                table: "properties",
                newName: "PurchaseDate");

            migrationBuilder.AddColumn<int>(
                name: "ConstructionYear",
                table: "properties",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnergyClass",
                table: "properties",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasBalcony",
                table: "properties",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConstructionYear",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "EnergyClass",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "HasBalcony",
                table: "properties");

            migrationBuilder.RenameColumn(
                name: "PurchaseDate",
                table: "properties",
                newName: "AcquisitionDate");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "properties",
                newName: "Notes");
        }
    }
}
