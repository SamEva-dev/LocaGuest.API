using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PropertyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RenterTenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Rent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Deposit = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contracts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Number = table.Column<int>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SubscriptionPlan = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    SubscriptionExpiryDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    AnnualPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxScenarios = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxExportsPerMonth = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxVersionsPerScenario = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxShares = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxAiSuggestionsPerMonth = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxWorkspaces = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxTeamMembers = table.Column<int>(type: "INTEGER", nullable: false),
                    HasUnlimitedExports = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasUnlimitedVersioning = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasUnlimitedAi = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasPrivateTemplates = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasTeamTemplates = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasAdvancedComparison = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasApiAccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasApiReadWrite = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasEmailNotifications = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasSlackIntegration = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasWebhooks = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasSso = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasPrioritySupport = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasDedicatedSupport = table.Column<bool>(type: "INTEGER", nullable: false),
                    StripeMonthlyPriceId = table.Column<string>(type: "TEXT", nullable: true),
                    StripeAnnualPriceId = table.Column<string>(type: "TEXT", nullable: true),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "properties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ZipCode = table.Column<string>(type: "TEXT", nullable: true),
                    Country = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Rent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Bedrooms = table.Column<int>(type: "INTEGER", nullable: false),
                    Bathrooms = table.Column<int>(type: "INTEGER", nullable: false),
                    Surface = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    HasElevator = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasParking = table.Column<bool>(type: "INTEGER", nullable: false),
                    Floor = table.Column<int>(type: "INTEGER", nullable: true),
                    IsFurnished = table.Column<bool>(type: "INTEGER", nullable: false),
                    Charges = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Deposit = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    ImageUrls = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_properties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "rentability_scenarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsBase = table.Column<bool>(type: "INTEGER", nullable: false),
                    PropertyType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Location = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Surface = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    State = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Strategy = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Horizon = table.Column<int>(type: "INTEGER", nullable: false),
                    Objective = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NotaryFees = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RenovationCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LandValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FurnitureCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MonthlyRent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Indexation = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IndexationRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    VacancyRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    SeasonalityEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    HighSeasonMultiplier = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    ParkingRent = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    StorageRent = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OtherRevenues = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GuaranteedRent = table.Column<bool>(type: "INTEGER", nullable: false),
                    RelocationIncrease = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    CondoFees = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Insurance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PropertyTax = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ManagementFees = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MaintenanceRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    RecoverableCharges = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ChargesIncrease = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    PlannedCapexJson = table.Column<string>(type: "text", nullable: true),
                    LoanAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LoanType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    InterestRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Duration = table.Column<int>(type: "INTEGER", nullable: false),
                    InsuranceRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    DeferredMonths = table.Column<int>(type: "INTEGER", nullable: false),
                    DeferredType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    EarlyRepaymentPenalty = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    IncludeNotaryInLoan = table.Column<bool>(type: "INTEGER", nullable: false),
                    IncludeRenovationInLoan = table.Column<bool>(type: "INTEGER", nullable: false),
                    TaxRegime = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    MarginalTaxRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    SocialContributions = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    DepreciationYears = table.Column<int>(type: "INTEGER", nullable: true),
                    FurnitureDepreciationYears = table.Column<int>(type: "INTEGER", nullable: true),
                    DeficitCarryForward = table.Column<bool>(type: "INTEGER", nullable: false),
                    CrlApplicable = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExitMethod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    TargetCapRate = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    AnnualAppreciation = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    TargetPricePerSqm = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SellingCosts = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CapitalGainsTax = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    HoldYears = table.Column<int>(type: "INTEGER", nullable: false),
                    ResultsJson = table.Column<string>(type: "text", nullable: true),
                    Tags = table.Column<string>(type: "TEXT", nullable: true),
                    Category = table.Column<string>(type: "TEXT", nullable: true),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false),
                    CurrentVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rentability_scenarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_sequences",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityPrefix = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    LastNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_sequences", x => new { x.TenantId, x.EntityPrefix });
                });

            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    MoveInDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tracking_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PageName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Url = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 45, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    SessionId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DurationMs = table.Column<int>(type: "INTEGER", nullable: true),
                    HttpStatusCode = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tracking_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "usage_aggregates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimension = table.Column<string>(type: "TEXT", nullable: false),
                    PeriodYear = table.Column<int>(type: "INTEGER", nullable: false),
                    PeriodMonth = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalValue = table.Column<int>(type: "INTEGER", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usage_aggregates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PhotoUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    EmailAlerts = table.Column<bool>(type: "INTEGER", nullable: false),
                    SmsAlerts = table.Column<bool>(type: "INTEGER", nullable: false),
                    NewReservations = table.Column<bool>(type: "INTEGER", nullable: false),
                    PaymentReminders = table.Column<bool>(type: "INTEGER", nullable: false),
                    MonthlyReports = table.Column<bool>(type: "INTEGER", nullable: false),
                    DarkMode = table.Column<bool>(type: "INTEGER", nullable: false),
                    Language = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Timezone = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DateFormat = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    SidebarNavigation = table.Column<bool>(type: "INTEGER", nullable: false),
                    HeaderNavigation = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowTracking = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContractId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Method = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payments_contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    IsAnnual = table.Column<bool>(type: "INTEGER", nullable: false),
                    CurrentPeriodStart = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CurrentPeriodEnd = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TrialEndsAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CanceledAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CancelAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    StripeCustomerId = table.Column<string>(type: "TEXT", nullable: true),
                    StripeSubscriptionId = table.Column<string>(type: "TEXT", nullable: true),
                    StripeLatestInvoiceId = table.Column<string>(type: "TEXT", nullable: true),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_subscriptions_plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scenario_shares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScenarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OwnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SharedWithUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Permission = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scenario_shares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_scenario_shares_rentability_scenarios_ScenarioId",
                        column: x => x.ScenarioId,
                        principalTable: "rentability_scenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scenario_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScenarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VersionNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    ChangeDescription = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SnapshotJson = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scenario_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_scenario_versions_rentability_scenarios_ScenarioId",
                        column: x => x.ScenarioId,
                        principalTable: "rentability_scenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScenarioComment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScenarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    ParentCommentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsEdited = table.Column<bool>(type: "INTEGER", nullable: false),
                    EditedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RentabilityScenarioId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScenarioComment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScenarioComment_rentability_scenarios_RentabilityScenarioId",
                        column: x => x.RentabilityScenarioId,
                        principalTable: "rentability_scenarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "usage_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dimension = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<int>(type: "INTEGER", nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "TEXT", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usage_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_usage_events_subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_contracts_RenterTenantId",
                table: "contracts",
                column: "RenterTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_contracts_TenantId",
                table: "contracts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_organizations_Code",
                table: "organizations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organizations_Email",
                table: "organizations",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_organizations_Number",
                table: "organizations",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_ContractId",
                table: "payments",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_plans_Code",
                table: "plans",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_properties_TenantId",
                table: "properties",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_rentability_scenarios_TenantId",
                table: "rentability_scenarios",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_rentability_scenarios_TenantId_UserId",
                table: "rentability_scenarios",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_rentability_scenarios_TenantId_UserId_IsBase",
                table: "rentability_scenarios",
                columns: new[] { "TenantId", "UserId", "IsBase" });

            migrationBuilder.CreateIndex(
                name: "IX_scenario_shares_ScenarioId",
                table: "scenario_shares",
                column: "ScenarioId");

            migrationBuilder.CreateIndex(
                name: "IX_scenario_shares_ScenarioId_SharedWithUserId",
                table: "scenario_shares",
                columns: new[] { "ScenarioId", "SharedWithUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_scenario_shares_SharedWithUserId",
                table: "scenario_shares",
                column: "SharedWithUserId");

            migrationBuilder.CreateIndex(
                name: "IX_scenario_versions_ScenarioId",
                table: "scenario_versions",
                column: "ScenarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioComment_RentabilityScenarioId",
                table: "ScenarioComment",
                column: "RentabilityScenarioId");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_PlanId",
                table: "subscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_StripeSubscriptionId",
                table: "subscriptions",
                column: "StripeSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_TenantId_UserId",
                table: "subscriptions",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_sequences_TenantId",
                table: "tenant_sequences",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_TenantId",
                table: "tenants",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_tracking_events_EventType",
                table: "tracking_events",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_tracking_events_EventType_Timestamp",
                table: "tracking_events",
                columns: new[] { "EventType", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_tracking_events_TenantId",
                table: "tracking_events",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_tracking_events_TenantId_Timestamp",
                table: "tracking_events",
                columns: new[] { "TenantId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_tracking_events_TenantId_UserId_Timestamp",
                table: "tracking_events",
                columns: new[] { "TenantId", "UserId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_tracking_events_Timestamp",
                table: "tracking_events",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_tracking_events_UserId",
                table: "tracking_events",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_usage_aggregates_TenantId_UserId_Dimension_PeriodYear_PeriodMonth",
                table: "usage_aggregates",
                columns: new[] { "TenantId", "UserId", "Dimension", "PeriodYear", "PeriodMonth" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usage_events_SubscriptionId",
                table: "usage_events",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_usage_events_TenantId_SubscriptionId",
                table: "usage_events",
                columns: new[] { "TenantId", "SubscriptionId" });

            migrationBuilder.CreateIndex(
                name: "IX_user_settings_TenantId_UserId",
                table: "user_settings",
                columns: new[] { "TenantId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "organizations");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "properties");

            migrationBuilder.DropTable(
                name: "scenario_shares");

            migrationBuilder.DropTable(
                name: "scenario_versions");

            migrationBuilder.DropTable(
                name: "ScenarioComment");

            migrationBuilder.DropTable(
                name: "tenant_sequences");

            migrationBuilder.DropTable(
                name: "tenants");

            migrationBuilder.DropTable(
                name: "tracking_events");

            migrationBuilder.DropTable(
                name: "usage_aggregates");

            migrationBuilder.DropTable(
                name: "usage_events");

            migrationBuilder.DropTable(
                name: "user_settings");

            migrationBuilder.DropTable(
                name: "contracts");

            migrationBuilder.DropTable(
                name: "rentability_scenarios");

            migrationBuilder.DropTable(
                name: "subscriptions");

            migrationBuilder.DropTable(
                name: "plans");
        }
    }
}
