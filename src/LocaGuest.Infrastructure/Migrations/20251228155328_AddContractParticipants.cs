using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContractParticipants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContractParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ShareType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ShareValue = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractParticipants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContractParticipants_ContractId",
                table: "ContractParticipants",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractParticipants_ContractId_TenantId_StartDate",
                table: "ContractParticipants",
                columns: new[] { "ContractId", "TenantId", "StartDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractParticipants_EndDate",
                table: "ContractParticipants",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_ContractParticipants_StartDate",
                table: "ContractParticipants",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_ContractParticipants_TenantId",
                table: "ContractParticipants",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractParticipants");
        }
    }
}
