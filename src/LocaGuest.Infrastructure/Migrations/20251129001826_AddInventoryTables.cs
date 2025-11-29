using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PropertyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RoomId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ContractId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", maxLength: 100, nullable: false),
                    InspectionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AgentName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TenantPresent = table.Column<bool>(type: "INTEGER", nullable: false),
                    RepresentativeName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    GeneralObservations = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    PhotoUrls = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "inventory_exits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PropertyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RoomId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ContractId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", maxLength: 100, nullable: false),
                    InventoryEntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InspectionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AgentName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TenantPresent = table.Column<bool>(type: "INTEGER", nullable: false),
                    RepresentativeName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    GeneralObservations = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    PhotoUrls = table.Column<string>(type: "TEXT", nullable: false),
                    TotalDeductionAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OwnerCoveredAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FinancialNotes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_exits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "inventory_items",
                columns: table => new
                {
                    RoomName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ElementName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    InventoryEntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Condition = table.Column<int>(type: "INTEGER", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PhotoUrls = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_items", x => new { x.InventoryEntryId, x.RoomName, x.ElementName });
                    table.ForeignKey(
                        name: "FK_inventory_items_inventory_entries_InventoryEntryId",
                        column: x => x.InventoryEntryId,
                        principalTable: "inventory_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventory_comparisons",
                columns: table => new
                {
                    RoomName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ElementName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    InventoryExitId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntryCondition = table.Column<int>(type: "INTEGER", nullable: false),
                    ExitCondition = table.Column<int>(type: "INTEGER", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PhotoUrls = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_comparisons", x => new { x.InventoryExitId, x.RoomName, x.ElementName });
                    table.ForeignKey(
                        name: "FK_inventory_comparisons_inventory_exits_InventoryExitId",
                        column: x => x.InventoryExitId,
                        principalTable: "inventory_exits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventory_degradations",
                columns: table => new
                {
                    RoomName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ElementName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    InventoryExitId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsImputedToTenant = table.Column<bool>(type: "INTEGER", nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PhotoUrls = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_degradations", x => new { x.InventoryExitId, x.RoomName, x.ElementName });
                    table.ForeignKey(
                        name: "FK_inventory_degradations_inventory_exits_InventoryExitId",
                        column: x => x.InventoryExitId,
                        principalTable: "inventory_exits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_entries_ContractId",
                table: "inventory_entries",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_entries_TenantId",
                table: "inventory_entries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_exits_ContractId",
                table: "inventory_exits",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_exits_InventoryEntryId",
                table: "inventory_exits",
                column: "InventoryEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_exits_TenantId",
                table: "inventory_exits",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_comparisons");

            migrationBuilder.DropTable(
                name: "inventory_degradations");

            migrationBuilder.DropTable(
                name: "inventory_items");

            migrationBuilder.DropTable(
                name: "inventory_exits");

            migrationBuilder.DropTable(
                name: "inventory_entries");
        }
    }
}
