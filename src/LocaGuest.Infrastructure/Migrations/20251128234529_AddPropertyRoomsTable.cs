using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyRoomsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "property_rooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PropertyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Surface = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Rent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Charges = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentContractId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_property_rooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_property_rooms_properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_property_rooms_CurrentContractId",
                table: "property_rooms",
                column: "CurrentContractId");

            migrationBuilder.CreateIndex(
                name: "IX_property_rooms_PropertyId",
                table: "property_rooms",
                column: "PropertyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "property_rooms");
        }
    }
}
