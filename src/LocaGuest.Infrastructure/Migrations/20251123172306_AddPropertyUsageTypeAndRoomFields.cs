using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyUsageTypeAndRoomFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaximumStay",
                table: "properties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinimumStay",
                table: "properties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OccupiedRooms",
                table: "properties",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerNight",
                table: "properties",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalRooms",
                table: "properties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsageType",
                table: "properties",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaximumStay",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "MinimumStay",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "OccupiedRooms",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "PricePerNight",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "TotalRooms",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "UsageType",
                table: "properties");
        }
    }
}
