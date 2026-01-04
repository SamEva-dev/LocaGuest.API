using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocaGuest.Infrastructure.Persistence.Migrations.Main
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "lease");

            migrationBuilder.EnsureSchema(
                name: "finance");

            migrationBuilder.EnsureSchema(
                name: "doc");

            migrationBuilder.EnsureSchema(
                name: "ops");

            migrationBuilder.EnsureSchema(
                name: "inventory");

            migrationBuilder.EnsureSchema(
                name: "org");

            migrationBuilder.EnsureSchema(
                name: "iam");

            migrationBuilder.EnsureSchema(
                name: "locaguest");

            migrationBuilder.EnsureSchema(
                name: "billing");

            migrationBuilder.EnsureSchema(
                name: "analytics");

            migrationBuilder.CreateSequence(
                name: "organization_number_seq",
                schema: "org");

            migrationBuilder.CreateTable(
                name: "contract_participants",
                schema: "lease",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    RenterTenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ShareType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ShareValue = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contract_participants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "contracts",
                schema: "lease",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    RenterTenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Rent = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Charges = table.Column<decimal>(type: "numeric", nullable: false),
                    Deposit = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    PaymentDueDay = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    TerminationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TerminationReason = table.Column<string>(type: "text", nullable: true),
                    NoticeDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NoticeEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NoticeReason = table.Column<string>(type: "text", nullable: true),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsConflict = table.Column<bool>(type: "boolean", nullable: false),
                    RenewedContractId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomClauses = table.Column<string>(type: "text", nullable: true),
                    PreviousIRL = table.Column<decimal>(type: "numeric", nullable: true),
                    CurrentIRL = table.Column<decimal>(type: "numeric", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contracts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "documents",
                schema: "doc",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssociatedTenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SignedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SignedBy = table.Column<string>(type: "text", nullable: true),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "idempotency_requests",
                schema: "ops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<string>(type: "text", nullable: false),
                    idempotency_key = table.Column<string>(type: "text", nullable: false),
                    request_hash = table.Column<string>(type: "text", nullable: false),
                    response_json = table.Column<string>(type: "text", nullable: false),
                    status_code = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_requests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "inventory_entries",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    RenterTenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AgentName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TenantPresent = table.Column<bool>(type: "boolean", nullable: false),
                    RepresentativeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GeneralObservations = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PhotoUrls = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IsFinalized = table.Column<bool>(type: "boolean", nullable: false),
                    FinalizedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "inventory_exits",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    RenterTenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AgentName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TenantPresent = table.Column<bool>(type: "boolean", nullable: false),
                    RepresentativeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GeneralObservations = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PhotoUrls = table.Column<string>(type: "text", nullable: false),
                    TotalDeductionAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OwnerCoveredAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    FinancialNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IsFinalized = table.Column<bool>(type: "boolean", nullable: false),
                    FinalizedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_exits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "invitations",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SecretHash = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcceptedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invitations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "notification_settings",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PaymentReceived = table.Column<bool>(type: "boolean", nullable: false),
                    PaymentOverdue = table.Column<bool>(type: "boolean", nullable: false),
                    PaymentReminder = table.Column<bool>(type: "boolean", nullable: false),
                    ContractSigned = table.Column<bool>(type: "boolean", nullable: false),
                    ContractExpiring = table.Column<bool>(type: "boolean", nullable: false),
                    ContractRenewal = table.Column<bool>(type: "boolean", nullable: false),
                    NewTenantRequest = table.Column<bool>(type: "boolean", nullable: false),
                    TenantCheckout = table.Column<bool>(type: "boolean", nullable: false),
                    MaintenanceRequest = table.Column<bool>(type: "boolean", nullable: false),
                    MaintenanceCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    SystemUpdates = table.Column<bool>(type: "boolean", nullable: false),
                    MarketingEmails = table.Column<bool>(type: "boolean", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "occupants",
                schema: "locaguest",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MoveInDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    PostalCode = table.Column<string>(type: "text", nullable: true),
                    Country = table.Column<string>(type: "text", nullable: true),
                    Nationality = table.Column<string>(type: "text", nullable: true),
                    IdNumber = table.Column<string>(type: "text", nullable: true),
                    EmergencyContact = table.Column<string>(type: "text", nullable: true),
                    EmergencyPhone = table.Column<string>(type: "text", nullable: true),
                    Occupation = table.Column<string>(type: "text", nullable: true),
                    MonthlyIncome = table.Column<decimal>(type: "numeric", nullable: true),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: true),
                    PropertyCode = table.Column<string>(type: "text", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_occupants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "organizations",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SubscriptionPlan = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SubscriptionExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    PrimaryColor = table.Column<string>(type: "text", nullable: true),
                    SecondaryColor = table.Column<string>(type: "text", nullable: true),
                    AccentColor = table.Column<string>(type: "text", nullable: true),
                    Website = table.Column<string>(type: "text", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RenterTenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AmountDue = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpectedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvoiceDocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "plans",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AnnualPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    MaxScenarios = table.Column<int>(type: "integer", nullable: false),
                    MaxExportsPerMonth = table.Column<int>(type: "integer", nullable: false),
                    MaxVersionsPerScenario = table.Column<int>(type: "integer", nullable: false),
                    MaxShares = table.Column<int>(type: "integer", nullable: false),
                    MaxAiSuggestionsPerMonth = table.Column<int>(type: "integer", nullable: false),
                    MaxWorkspaces = table.Column<int>(type: "integer", nullable: false),
                    MaxTeamMembers = table.Column<int>(type: "integer", nullable: false),
                    HasUnlimitedExports = table.Column<bool>(type: "boolean", nullable: false),
                    HasUnlimitedVersioning = table.Column<bool>(type: "boolean", nullable: false),
                    HasUnlimitedAi = table.Column<bool>(type: "boolean", nullable: false),
                    HasPrivateTemplates = table.Column<bool>(type: "boolean", nullable: false),
                    HasTeamTemplates = table.Column<bool>(type: "boolean", nullable: false),
                    HasAdvancedComparison = table.Column<bool>(type: "boolean", nullable: false),
                    HasApiAccess = table.Column<bool>(type: "boolean", nullable: false),
                    HasApiReadWrite = table.Column<bool>(type: "boolean", nullable: false),
                    HasEmailNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    HasSlackIntegration = table.Column<bool>(type: "boolean", nullable: false),
                    HasWebhooks = table.Column<bool>(type: "boolean", nullable: false),
                    HasSso = table.Column<bool>(type: "boolean", nullable: false),
                    HasPrioritySupport = table.Column<bool>(type: "boolean", nullable: false),
                    HasDedicatedSupport = table.Column<bool>(type: "boolean", nullable: false),
                    StripeMonthlyPriceId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    StripeAnnualPriceId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "properties",
                schema: "locaguest",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PostalCode = table.Column<string>(type: "text", nullable: true),
                    Country = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    UsageType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Rent = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalRooms = table.Column<int>(type: "integer", nullable: true),
                    OccupiedRooms = table.Column<int>(type: "integer", nullable: false),
                    ReservedRooms = table.Column<int>(type: "integer", nullable: false),
                    MinimumStay = table.Column<int>(type: "integer", nullable: true),
                    MaximumStay = table.Column<int>(type: "integer", nullable: true),
                    PricePerNight = table.Column<decimal>(type: "numeric", nullable: true),
                    Bedrooms = table.Column<int>(type: "integer", nullable: false),
                    Bathrooms = table.Column<int>(type: "integer", nullable: false),
                    Surface = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    HasElevator = table.Column<bool>(type: "boolean", nullable: false),
                    HasParking = table.Column<bool>(type: "boolean", nullable: false),
                    HasBalcony = table.Column<bool>(type: "boolean", nullable: false),
                    Floor = table.Column<int>(type: "integer", nullable: true),
                    IsFurnished = table.Column<bool>(type: "boolean", nullable: false),
                    Charges = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Deposit = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ImageUrls = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EnergyClass = table.Column<string>(type: "text", nullable: true),
                    ConstructionYear = table.Column<int>(type: "integer", nullable: true),
                    DpeRating = table.Column<string>(type: "text", nullable: true),
                    DpeValue = table.Column<int>(type: "integer", nullable: true),
                    GesRating = table.Column<string>(type: "text", nullable: true),
                    ElectricDiagnosticDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ElectricDiagnosticExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GasDiagnosticDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GasDiagnosticExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HasAsbestos = table.Column<bool>(type: "boolean", nullable: true),
                    AsbestosDiagnosticDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErpZone = table.Column<string>(type: "text", nullable: true),
                    PropertyTax = table.Column<decimal>(type: "numeric", nullable: true),
                    CondominiumCharges = table.Column<decimal>(type: "numeric", nullable: true),
                    PurchasePrice = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Insurance = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    ManagementFeesRate = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    MaintenanceRate = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    VacancyRate = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    NightsBookedPerMonth = table.Column<int>(type: "integer", nullable: true),
                    CadastralReference = table.Column<string>(type: "text", nullable: true),
                    LotNumber = table.Column<string>(type: "text", nullable: true),
                    PurchaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalWorksAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    AssociatedTenantCodes = table.Column<List<string>>(type: "text[]", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_properties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "rent_invoice_lines",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RentInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    RenterTenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmountDue = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ShareType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ShareValue = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaidDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rent_invoice_lines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "rent_invoices",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    RenterTenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceDocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaidDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rent_invoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "rentability_scenarios",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
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
                    Tags = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: true),
                    IsFavorite = table.Column<bool>(type: "boolean", nullable: false),
                    CurrentVersion = table.Column<int>(type: "integer", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rentability_scenarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_sequences",
                schema: "org",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityPrefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    LastNumber = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_sequences", x => new { x.OrganizationId, x.EntityPrefix });
                });

            migrationBuilder.CreateTable(
                name: "tracking_events",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PageName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    SessionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DurationMs = table.Column<int>(type: "integer", nullable: true),
                    HttpStatusCode = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tracking_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "usage_aggregates",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Dimension = table.Column<string>(type: "text", nullable: false),
                    PeriodYear = table.Column<int>(type: "integer", nullable: false),
                    PeriodMonth = table.Column<int>(type: "integer", nullable: false),
                    TotalValue = table.Column<int>(type: "integer", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usage_aggregates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_preferences",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DarkMode = table.Column<bool>(type: "boolean", nullable: false),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Timezone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DateFormat = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SidebarNavigation = table.Column<bool>(type: "boolean", nullable: false),
                    HeaderNavigation = table.Column<bool>(type: "boolean", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_preferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_profiles",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LastName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Company = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Bio = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PhotoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_sessions",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SessionToken = table.Column<string>(type: "text", nullable: false),
                    DeviceName = table.Column<string>(type: "text", nullable: false),
                    Browser = table.Column<string>(type: "text", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_settings",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PhotoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EmailAlerts = table.Column<bool>(type: "boolean", nullable: false),
                    SmsAlerts = table.Column<bool>(type: "boolean", nullable: false),
                    NewReservations = table.Column<bool>(type: "boolean", nullable: false),
                    PaymentReminders = table.Column<bool>(type: "boolean", nullable: false),
                    MonthlyReports = table.Column<bool>(type: "boolean", nullable: false),
                    DarkMode = table.Column<bool>(type: "boolean", nullable: false),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Timezone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DateFormat = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SidebarNavigation = table.Column<bool>(type: "boolean", nullable: false),
                    HeaderNavigation = table.Column<bool>(type: "boolean", nullable: false),
                    AllowTracking = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "addendums",
                schema: "lease",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    OldRent = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    NewRent = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    OldCharges = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    NewCharges = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    OldEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NewEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OccupantChanges = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    OldRoomId = table.Column<Guid>(type: "uuid", nullable: true),
                    NewRoomId = table.Column<Guid>(type: "uuid", nullable: true),
                    OldClauses = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    NewClauses = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AttachedDocumentIds = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SignatureStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SignedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_addendums", x => x.Id);
                    table.ForeignKey(
                        name: "FK_addendums_contracts_ContractId",
                        column: x => x.ContractId,
                        principalSchema: "lease",
                        principalTable: "contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contract_payments",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contract_payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_contract_payments_contracts_ContractId",
                        column: x => x.ContractId,
                        principalSchema: "lease",
                        principalTable: "contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "required_documents",
                schema: "lease",
                columns: table => new
                {
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    IsProvided = table.Column<bool>(type: "boolean", nullable: false),
                    IsSigned = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_required_documents", x => new { x.ContractId, x.Type });
                    table.ForeignKey(
                        name: "FK_required_documents_contracts_ContractId",
                        column: x => x.ContractId,
                        principalSchema: "lease",
                        principalTable: "contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventory_items",
                schema: "inventory",
                columns: table => new
                {
                    RoomName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ElementName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InventoryEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Condition = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PhotoUrls = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_items", x => new { x.InventoryEntryId, x.RoomName, x.ElementName });
                    table.ForeignKey(
                        name: "FK_inventory_items_inventory_entries_InventoryEntryId",
                        column: x => x.InventoryEntryId,
                        principalSchema: "inventory",
                        principalTable: "inventory_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventory_comparisons",
                schema: "inventory",
                columns: table => new
                {
                    RoomName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ElementName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InventoryExitId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntryCondition = table.Column<int>(type: "integer", nullable: false),
                    ExitCondition = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PhotoUrls = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_comparisons", x => new { x.InventoryExitId, x.RoomName, x.ElementName });
                    table.ForeignKey(
                        name: "FK_inventory_comparisons_inventory_exits_InventoryExitId",
                        column: x => x.InventoryExitId,
                        principalSchema: "inventory",
                        principalTable: "inventory_exits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventory_degradations",
                schema: "inventory",
                columns: table => new
                {
                    RoomName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ElementName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InventoryExitId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsImputedToTenant = table.Column<bool>(type: "boolean", nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PhotoUrls = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_degradations", x => new { x.InventoryExitId, x.RoomName, x.ElementName });
                    table.ForeignKey(
                        name: "FK_inventory_degradations_inventory_exits_InventoryExitId",
                        column: x => x.InventoryExitId,
                        principalSchema: "inventory",
                        principalTable: "inventory_exits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "team_members",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UserEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InvitedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    InvitedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_team_members_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "org",
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    IsAnnual = table.Column<bool>(type: "boolean", nullable: false),
                    CurrentPeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentPeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TrialEndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CanceledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StripeCustomerId = table.Column<string>(type: "text", nullable: true),
                    StripeSubscriptionId = table.Column<string>(type: "text", nullable: true),
                    StripeLatestInvoiceId = table.Column<string>(type: "text", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_subscriptions_plans_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "billing",
                        principalTable: "plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "property_images",
                schema: "locaguest",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    MimeType = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_property_images", x => x.Id);
                    table.ForeignKey(
                        name: "FK_property_images_properties_PropertyId",
                        column: x => x.PropertyId,
                        principalSchema: "locaguest",
                        principalTable: "properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "property_rooms",
                schema: "locaguest",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Surface = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Rent = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Charges = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OnHoldUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CurrentContractId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_property_rooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_property_rooms_properties_PropertyId",
                        column: x => x.PropertyId,
                        principalSchema: "locaguest",
                        principalTable: "properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scenario_shares",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScenarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SharedWithUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Permission = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scenario_shares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_scenario_shares_rentability_scenarios_ScenarioId",
                        column: x => x.ScenarioId,
                        principalSchema: "analytics",
                        principalTable: "rentability_scenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scenario_versions",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScenarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    ChangeDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SnapshotJson = table.Column<string>(type: "text", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scenario_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_scenario_versions_rentability_scenarios_ScenarioId",
                        column: x => x.ScenarioId,
                        principalSchema: "analytics",
                        principalTable: "rentability_scenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScenarioComment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScenarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ParentCommentId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsEdited = table.Column<bool>(type: "boolean", nullable: false),
                    EditedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RentabilityScenarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScenarioComment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScenarioComment_rentability_scenarios_RentabilityScenarioId",
                        column: x => x.RentabilityScenarioId,
                        principalSchema: "analytics",
                        principalTable: "rentability_scenarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "invitation_tokens",
                schema: "org",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invitation_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invitation_tokens_team_members_TeamMemberId",
                        column: x => x.TeamMemberId,
                        principalSchema: "org",
                        principalTable: "team_members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usage_events",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Dimension = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<int>(type: "integer", nullable: false),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usage_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_usage_events_subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalSchema: "billing",
                        principalTable: "subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_addendums_ContractId",
                schema: "lease",
                table: "addendums",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_addendums_CreatedAt",
                schema: "lease",
                table: "addendums",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_addendums_EffectiveDate",
                schema: "lease",
                table: "addendums",
                column: "EffectiveDate");

            migrationBuilder.CreateIndex(
                name: "IX_addendums_SignatureStatus",
                schema: "lease",
                table: "addendums",
                column: "SignatureStatus");

            migrationBuilder.CreateIndex(
                name: "IX_addendums_Type",
                schema: "lease",
                table: "addendums",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_contract_participants_ContractId",
                schema: "lease",
                table: "contract_participants",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_contract_participants_ContractId_OrganizationId_StartDate",
                schema: "lease",
                table: "contract_participants",
                columns: new[] { "ContractId", "OrganizationId", "StartDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_contract_participants_EndDate",
                schema: "lease",
                table: "contract_participants",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_contract_participants_OrganizationId",
                schema: "lease",
                table: "contract_participants",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_contract_participants_StartDate",
                schema: "lease",
                table: "contract_participants",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_contract_payments_ContractId",
                schema: "finance",
                table: "contract_payments",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_contracts_OrganizationId",
                schema: "lease",
                table: "contracts",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_contracts_RenterTenantId",
                schema: "lease",
                table: "contracts",
                column: "RenterTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_documents_AssociatedTenantId",
                schema: "doc",
                table: "documents",
                column: "AssociatedTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_documents_Category",
                schema: "doc",
                table: "documents",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_documents_Code",
                schema: "doc",
                table: "documents",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_documents_IsArchived",
                schema: "doc",
                table: "documents",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_documents_PropertyId",
                schema: "doc",
                table: "documents",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_documents_Type",
                schema: "doc",
                table: "documents",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_idempotency_requests_client_id_idempotency_key",
                schema: "ops",
                table: "idempotency_requests",
                columns: new[] { "client_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_entries_ContractId",
                schema: "inventory",
                table: "inventory_entries",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_entries_OrganizationId",
                schema: "inventory",
                table: "inventory_entries",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_entries_RenterTenantId",
                schema: "inventory",
                table: "inventory_entries",
                column: "RenterTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_exits_ContractId",
                schema: "inventory",
                table: "inventory_exits",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_exits_InventoryEntryId",
                schema: "inventory",
                table: "inventory_exits",
                column: "InventoryEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_exits_OrganizationId",
                schema: "inventory",
                table: "inventory_exits",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_exits_RenterTenantId",
                schema: "inventory",
                table: "inventory_exits",
                column: "RenterTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_invitation_tokens_Email",
                schema: "org",
                table: "invitation_tokens",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_invitation_tokens_OrganizationId",
                schema: "org",
                table: "invitation_tokens",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_invitation_tokens_TeamMemberId",
                schema: "org",
                table: "invitation_tokens",
                column: "TeamMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_invitation_tokens_Token",
                schema: "org",
                table: "invitation_tokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invitations_ExpiresAtUtc",
                schema: "org",
                table: "invitations",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_invitations_OrganizationId_Email_Status",
                schema: "org",
                table: "invitations",
                columns: new[] { "OrganizationId", "Email", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_settings_OrganizationId_UserId",
                schema: "iam",
                table: "notification_settings",
                columns: new[] { "OrganizationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_occupants_OrganizationId",
                schema: "locaguest",
                table: "occupants",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_organizations_Code",
                schema: "org",
                table: "organizations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organizations_Email",
                schema: "org",
                table: "organizations",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_organizations_Number",
                schema: "org",
                table: "organizations",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_ContractId",
                schema: "finance",
                table: "payments",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_ContractId_Month_Year_PaymentType",
                schema: "finance",
                table: "payments",
                columns: new[] { "ContractId", "Month", "Year", "PaymentType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_OrganizationId",
                schema: "finance",
                table: "payments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_PropertyId",
                schema: "finance",
                table: "payments",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_Status",
                schema: "finance",
                table: "payments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_plans_Code",
                schema: "billing",
                table: "plans",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_plans_SortOrder",
                schema: "billing",
                table: "plans",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_properties_OrganizationId",
                schema: "locaguest",
                table: "properties",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_property_images_PropertyId",
                schema: "locaguest",
                table: "property_images",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_property_rooms_CurrentContractId",
                schema: "locaguest",
                table: "property_rooms",
                column: "CurrentContractId");

            migrationBuilder.CreateIndex(
                name: "IX_property_rooms_OnHoldUntilUtc",
                schema: "locaguest",
                table: "property_rooms",
                column: "OnHoldUntilUtc");

            migrationBuilder.CreateIndex(
                name: "IX_property_rooms_PropertyId",
                schema: "locaguest",
                table: "property_rooms",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_rent_invoice_lines_OrganizationId",
                schema: "finance",
                table: "rent_invoice_lines",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_rent_invoice_lines_RentInvoiceId",
                schema: "finance",
                table: "rent_invoice_lines",
                column: "RentInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_rent_invoice_lines_RentInvoiceId_OrganizationId",
                schema: "finance",
                table: "rent_invoice_lines",
                columns: new[] { "RentInvoiceId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rent_invoice_lines_Status",
                schema: "finance",
                table: "rent_invoice_lines",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_rent_invoices_ContractId",
                schema: "finance",
                table: "rent_invoices",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_rent_invoices_ContractId_Month_Year",
                schema: "finance",
                table: "rent_invoices",
                columns: new[] { "ContractId", "Month", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rent_invoices_DueDate",
                schema: "finance",
                table: "rent_invoices",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_rent_invoices_OrganizationId",
                schema: "finance",
                table: "rent_invoices",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_rent_invoices_PropertyId",
                schema: "finance",
                table: "rent_invoices",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_rent_invoices_Status",
                schema: "finance",
                table: "rent_invoices",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_rentability_scenarios_OrganizationId",
                schema: "analytics",
                table: "rentability_scenarios",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_rentability_scenarios_OrganizationId_UserId",
                schema: "analytics",
                table: "rentability_scenarios",
                columns: new[] { "OrganizationId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_rentability_scenarios_OrganizationId_UserId_IsBase",
                schema: "analytics",
                table: "rentability_scenarios",
                columns: new[] { "OrganizationId", "UserId", "IsBase" });

            migrationBuilder.CreateIndex(
                name: "IX_scenario_shares_ScenarioId",
                schema: "analytics",
                table: "scenario_shares",
                column: "ScenarioId");

            migrationBuilder.CreateIndex(
                name: "IX_scenario_shares_ScenarioId_SharedWithUserId",
                schema: "analytics",
                table: "scenario_shares",
                columns: new[] { "ScenarioId", "SharedWithUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_scenario_shares_SharedWithUserId",
                schema: "analytics",
                table: "scenario_shares",
                column: "SharedWithUserId");

            migrationBuilder.CreateIndex(
                name: "IX_scenario_versions_ScenarioId",
                schema: "analytics",
                table: "scenario_versions",
                column: "ScenarioId");

            migrationBuilder.CreateIndex(
                name: "IX_ScenarioComment_RentabilityScenarioId",
                table: "ScenarioComment",
                column: "RentabilityScenarioId");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_OrganizationId_UserId",
                schema: "billing",
                table: "subscriptions",
                columns: new[] { "OrganizationId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_PlanId",
                schema: "billing",
                table: "subscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_StripeSubscriptionId",
                schema: "billing",
                table: "subscriptions",
                column: "StripeSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_team_members_OrganizationId",
                schema: "org",
                table: "team_members",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_team_members_UserEmail",
                schema: "org",
                table: "team_members",
                column: "UserEmail");

            migrationBuilder.CreateIndex(
                name: "IX_team_members_UserId_OrganizationId",
                schema: "org",
                table: "team_members",
                columns: new[] { "UserId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_sequences_OrganizationId",
                schema: "org",
                table: "tenant_sequences",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_tracking_events_EventType",
                schema: "analytics",
                table: "tracking_events",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_tracking_events_EventType_Timestamp",
                schema: "analytics",
                table: "tracking_events",
                columns: new[] { "EventType", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_tracking_events_OrganizationId",
                schema: "analytics",
                table: "tracking_events",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_tracking_events_OrganizationId_Timestamp",
                schema: "analytics",
                table: "tracking_events",
                columns: new[] { "OrganizationId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_tracking_events_OrganizationId_UserId_Timestamp",
                schema: "analytics",
                table: "tracking_events",
                columns: new[] { "OrganizationId", "UserId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_tracking_events_Timestamp",
                schema: "analytics",
                table: "tracking_events",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_tracking_events_UserId",
                schema: "analytics",
                table: "tracking_events",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_usage_aggregates_OrganizationId_UserId_Dimension_PeriodYear~",
                schema: "billing",
                table: "usage_aggregates",
                columns: new[] { "OrganizationId", "UserId", "Dimension", "PeriodYear", "PeriodMonth" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usage_events_OrganizationId_SubscriptionId",
                schema: "billing",
                table: "usage_events",
                columns: new[] { "OrganizationId", "SubscriptionId" });

            migrationBuilder.CreateIndex(
                name: "IX_usage_events_SubscriptionId",
                schema: "billing",
                table: "usage_events",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_user_preferences_OrganizationId_UserId",
                schema: "iam",
                table: "user_preferences",
                columns: new[] { "OrganizationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_profiles_OrganizationId_UserId",
                schema: "iam",
                table: "user_profiles",
                columns: new[] { "OrganizationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_settings_OrganizationId_UserId",
                schema: "iam",
                table: "user_settings",
                columns: new[] { "OrganizationId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "addendums",
                schema: "lease");

            migrationBuilder.DropTable(
                name: "contract_participants",
                schema: "lease");

            migrationBuilder.DropTable(
                name: "contract_payments",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "documents",
                schema: "doc");

            migrationBuilder.DropTable(
                name: "idempotency_requests",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "inventory_comparisons",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "inventory_degradations",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "inventory_items",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "invitation_tokens",
                schema: "org");

            migrationBuilder.DropTable(
                name: "invitations",
                schema: "org");

            migrationBuilder.DropTable(
                name: "notification_settings",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "occupants",
                schema: "locaguest");

            migrationBuilder.DropTable(
                name: "payments",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "property_images",
                schema: "locaguest");

            migrationBuilder.DropTable(
                name: "property_rooms",
                schema: "locaguest");

            migrationBuilder.DropTable(
                name: "rent_invoice_lines",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "rent_invoices",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "required_documents",
                schema: "lease");

            migrationBuilder.DropTable(
                name: "scenario_shares",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "scenario_versions",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "ScenarioComment");

            migrationBuilder.DropTable(
                name: "tenant_sequences",
                schema: "org");

            migrationBuilder.DropTable(
                name: "tracking_events",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "usage_aggregates",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "usage_events",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "user_preferences",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "user_profiles",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "user_sessions",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "user_settings",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "inventory_exits",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "inventory_entries",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "team_members",
                schema: "org");

            migrationBuilder.DropTable(
                name: "properties",
                schema: "locaguest");

            migrationBuilder.DropTable(
                name: "contracts",
                schema: "lease");

            migrationBuilder.DropTable(
                name: "rentability_scenarios",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "subscriptions",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "organizations",
                schema: "org");

            migrationBuilder.DropTable(
                name: "plans",
                schema: "billing");

            migrationBuilder.DropSequence(
                name: "organization_number_seq",
                schema: "org");
        }
    }
}
