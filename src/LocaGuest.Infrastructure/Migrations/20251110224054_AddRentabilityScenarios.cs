using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRentabilityScenarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rentability_scenarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsBase = table.Column<bool>(type: "boolean", nullable: false),
                    PropertyType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Location = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Surface = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    State = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Strategy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Horizon = table.Column<int>(type: "integer", nullable: false),
                    Objective = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NotaryFees = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RenovationCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LandValue = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    FurnitureCost = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    MonthlyRent = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Indexation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IndexationRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    VacancyRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    SeasonalityEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    HighSeasonMultiplier = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    ParkingRent = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    StorageRent = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    OtherRevenues = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    GuaranteedRent = table.Column<bool>(type: "boolean", nullable: false),
                    RelocationIncrease = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    CondoFees = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Insurance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PropertyTax = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ManagementFees = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    MaintenanceRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    RecoverableCharges = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ChargesIncrease = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    PlannedCapexJson = table.Column<string>(type: "text", nullable: true),
                    LoanAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LoanType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    InterestRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Duration = table.Column<int>(type: "integer", nullable: false),
                    InsuranceRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    DeferredMonths = table.Column<int>(type: "integer", nullable: false),
                    DeferredType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EarlyRepaymentPenalty = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    IncludeNotaryInLoan = table.Column<bool>(type: "boolean", nullable: false),
                    IncludeRenovationInLoan = table.Column<bool>(type: "boolean", nullable: false),
                    TaxRegime = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MarginalTaxRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    SocialContributions = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    DepreciationYears = table.Column<int>(type: "integer", nullable: true),
                    FurnitureDepreciationYears = table.Column<int>(type: "integer", nullable: true),
                    DeficitCarryForward = table.Column<bool>(type: "boolean", nullable: false),
                    CrlApplicable = table.Column<bool>(type: "boolean", nullable: false),
                    ExitMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetCapRate = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    AnnualAppreciation = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    TargetPricePerSqm = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    SellingCosts = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CapitalGainsTax = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    HoldYears = table.Column<int>(type: "integer", nullable: false),
                    ResultsJson = table.Column<string>(type: "text", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rentability_scenarios", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_rentability_scenarios_UserId",
                table: "rentability_scenarios",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_rentability_scenarios_UserId_IsBase",
                table: "rentability_scenarios",
                columns: new[] { "UserId", "IsBase" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rentability_scenarios");
        }
    }
}
