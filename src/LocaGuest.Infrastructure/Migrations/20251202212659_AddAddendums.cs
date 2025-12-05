using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAddendums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Addendums",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContractId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    OldRent = table.Column<decimal>(type: "TEXT", nullable: true),
                    NewRent = table.Column<decimal>(type: "TEXT", nullable: true),
                    OldCharges = table.Column<decimal>(type: "TEXT", nullable: true),
                    NewCharges = table.Column<decimal>(type: "TEXT", nullable: true),
                    OldEndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NewEndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    OccupantChanges = table.Column<string>(type: "TEXT", nullable: true),
                    OldRoomId = table.Column<Guid>(type: "TEXT", nullable: true),
                    NewRoomId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OldClauses = table.Column<string>(type: "TEXT", nullable: true),
                    NewClauses = table.Column<string>(type: "TEXT", nullable: true),
                    AttachedDocumentIds = table.Column<string>(type: "TEXT", nullable: true),
                    SignatureStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    SignedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Addendums", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Addendums_contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Addendums_ContractId",
                table: "Addendums",
                column: "ContractId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Addendums");
        }
    }
}
