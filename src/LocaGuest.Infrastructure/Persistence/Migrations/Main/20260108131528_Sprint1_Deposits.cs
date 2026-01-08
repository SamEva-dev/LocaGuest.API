using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Persistence.Migrations.Main
{
    /// <inheritdoc />
    public partial class Sprint1_Deposits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "deposits",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmountExpected = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AllowInstallments = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deposits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "deposit_transactions",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepositId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    DateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deposit_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_deposit_transactions_deposits_DepositId",
                        column: x => x.DepositId,
                        principalSchema: "finance",
                        principalTable: "deposits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_deposit_transactions_DateUtc",
                schema: "finance",
                table: "deposit_transactions",
                column: "DateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_deposit_transactions_DepositId",
                schema: "finance",
                table: "deposit_transactions",
                column: "DepositId");

            migrationBuilder.CreateIndex(
                name: "IX_deposits_ContractId",
                schema: "finance",
                table: "deposits",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_deposits_OrganizationId",
                schema: "finance",
                table: "deposits",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_deposits_OrganizationId_ContractId",
                schema: "finance",
                table: "deposits",
                columns: new[] { "OrganizationId", "ContractId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "deposit_transactions",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "deposits",
                schema: "finance");
        }
    }
}
