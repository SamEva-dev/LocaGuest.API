using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Aggregates.DocumentAggregate;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Aggregates.OccupantAggregate;
using LocaGuest.Domain.Aggregates.UserAggregate;
using LocaGuest.Domain.Aggregates.SubscriptionAggregate;
using LocaGuest.Domain.Aggregates.RentabilityAggregate;
using LocaGuest.Domain.Aggregates.OrganizationAggregate;
using LocaGuest.Domain.Aggregates.InventoryAggregate;
using LocaGuest.Domain.Aggregates.PaymentAggregate;
using LocaGuest.Domain.Entities;
using LocaGuest.Domain.Aggregates.AnalyticsAggregate;
using LocaGuest.Domain.Analytics;
using LocaGuest.Domain.Common;
using LocaGuest.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Reflection;
using System.Linq.Expressions;
using LocaGuest.Application.Services;
using LocaGuest.Infrastructure.Persistence.Entities;

namespace LocaGuest.Infrastructure.Persistence;

public class LocaGuestDbContext : DbContext, ILocaGuestDbContext, ILocaGuestReadDbContext
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService? _currentUserService;
    private readonly IOrganizationContext? _orgContext;

    public LocaGuestDbContext(
        DbContextOptions<LocaGuestDbContext> options,
        IMediator mediator,
        ICurrentUserService? currentUserService = null,
        IOrganizationContext? organizationContext = null)
        : base(options)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _orgContext = organizationContext;
    }

    // Multi-tenant Organizations
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationSequence> OrganizationSequences => Set<OrganizationSequence>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<InvitationToken> InvitationTokens => Set<InvitationToken>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<TenantOnboardingInvitation> TenantOnboardingInvitations => Set<TenantOnboardingInvitation>();

    public DbSet<IdempotencyRequestEntity> IdempotencyRequests => Set<IdempotencyRequestEntity>();

    public DbSet<Property> Properties => Set<Property>();
    public DbSet<PropertyRoom> PropertyRooms => Set<PropertyRoom>();
    public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
    public DbSet<Occupant> Occupants => Set<Occupant>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ContractParticipant> ContractParticipants => Set<ContractParticipant>();
    public DbSet<Addendum> Addendums => Set<Addendum>();
    public DbSet<ContractDocumentLink> ContractDocumentLinks => Set<ContractDocumentLink>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<LocaGuest.Domain.Aggregates.DepositAggregate.Deposit> Deposits => Set<LocaGuest.Domain.Aggregates.DepositAggregate.Deposit>();
    public DbSet<LocaGuest.Domain.Aggregates.DepositAggregate.DepositTransaction> DepositTransactions => Set<LocaGuest.Domain.Aggregates.DepositAggregate.DepositTransaction>();
    public DbSet<Domain.Aggregates.PaymentAggregate.Payment> Payments => Set<Domain.Aggregates.PaymentAggregate.Payment>();
    public DbSet<RentInvoice> RentInvoices => Set<RentInvoice>();
    public DbSet<LocaGuest.Domain.Aggregates.PaymentAggregate.RentInvoiceLine> RentInvoiceLines => Set<LocaGuest.Domain.Aggregates.PaymentAggregate.RentInvoiceLine>();
    public DbSet<InventoryEntry> InventoryEntries => Set<InventoryEntry>();
    public DbSet<InventoryExit> InventoryExits => Set<InventoryExit>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();
    public DbSet<NotificationSettings> NotificationSettings => Set<NotificationSettings>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<RentabilityScenario> RentabilityScenarios => Set<RentabilityScenario>();
    public DbSet<ScenarioVersion> ScenarioVersions => Set<ScenarioVersion>();
    public DbSet<ScenarioShare> ScenarioShares => Set<ScenarioShare>();
    
    // Subscription System
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<UsageEvent> UsageEvents => Set<UsageEvent>();
    public DbSet<UsageAggregate> UsageAggregates => Set<UsageAggregate>();
    
    // Analytics & Tracking
    public DbSet<TrackingEvent> TrackingEvents => Set<TrackingEvent>();
    public DbSet<SatisfactionFeedback> SatisfactionFeedbacks => Set<SatisfactionFeedback>();

    public DbSet<EmailDeliveryEvent> EmailDeliveryEvents => Set<EmailDeliveryEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasSequence<long>("organization_number_seq", schema: "org")
            .StartsAt(1)
            .IncrementsBy(1);

        var stringListComparer = new ValueComparer<List<string>>(
            (l1, l2) => (l1 ?? new List<string>()).SequenceEqual(l2 ?? new List<string>()),
            l => (l ?? new List<string>()).Aggregate(0, (a, v) => HashCode.Combine(a, v != null ? v.GetHashCode() : 0)),
            l => (l ?? new List<string>()).ToList()
        );

        // Configuration Global Query Filters pour isolation multi-tenant
        ConfigureMultiTenantFilters(modelBuilder);

        // Organization (Multi-tenant root entity)
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.ToTable("organizations", schema: "org");
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Number).IsRequired().ValueGeneratedNever(); // Manually managed
            entity.HasIndex(o => o.Number).IsUnique();
            entity.Property(o => o.Code).IsRequired().HasMaxLength(20);
            entity.HasIndex(o => o.Code).IsUnique();
            entity.Property(o => o.Name).IsRequired().HasMaxLength(200);
            entity.Property(o => o.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(o => o.Email);
            entity.Property(o => o.Phone).HasMaxLength(50);
            entity.Property(o => o.Status).IsRequired();
            entity.Property(o => o.SubscriptionPlan).HasMaxLength(50);
            entity.Property(o => o.Notes).HasMaxLength(1000);
            entity.Ignore(o => o.DomainEvents);
        });

        modelBuilder.Entity<ContractDocumentLink>(entity =>
        {
            entity.ToTable("contract_documents", schema: "lease");
            entity.HasKey(x => new { x.OrganizationId, x.ContractId, x.DocumentId });

            entity.Property(x => x.OrganizationId).IsRequired();
            entity.HasIndex(x => x.OrganizationId);
            entity.Property(x => x.ContractId).IsRequired();
            entity.HasIndex(x => x.ContractId);
            entity.Property(x => x.DocumentId).IsRequired();
            entity.HasIndex(x => x.DocumentId);

            entity.Property(x => x.Type)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(x => x.LinkedAtUtc).IsRequired();

            entity.HasOne<Contract>()
                .WithMany()
                .HasForeignKey(x => x.ContractId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Document>()
                .WithMany()
                .HasForeignKey(x => x.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // OrganizationSequence (Numbering service per organization)
        modelBuilder.Entity<OrganizationSequence>(entity =>
        {
            entity.ToTable("tenant_sequences", schema: "org");
            entity.HasKey(ts => new { ts.OrganizationId, ts.EntityPrefix });
            entity.Property(ts => ts.OrganizationId).IsRequired();
            entity.Property(ts => ts.EntityPrefix).IsRequired().HasMaxLength(10);
            entity.Property(ts => ts.LastNumber).IsRequired();
            entity.Property(ts => ts.Description).HasMaxLength(200);
            entity.HasIndex(ts => ts.OrganizationId);
        });

        // TeamMember (Members of an organization)
        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.ToTable("team_members", schema: "org");
            entity.HasKey(tm => tm.Id);
            entity.Property(tm => tm.UserId).IsRequired();
            entity.Property(tm => tm.OrganizationId).IsRequired();
            entity.Property(tm => tm.Role).IsRequired().HasMaxLength(50);
            entity.Property(tm => tm.UserEmail).IsRequired().HasMaxLength(200);
            entity.Property(tm => tm.InvitedBy);
            entity.Property(tm => tm.InvitedAt).IsRequired();
            entity.Property(tm => tm.AcceptedAt);
            entity.Property(tm => tm.IsActive).IsRequired();
            entity.Property(tm => tm.RemovedAt);
            
            entity.HasIndex(tm => tm.OrganizationId);
            entity.HasIndex(tm => new { tm.UserId, tm.OrganizationId }).IsUnique();
            entity.HasIndex(tm => tm.UserEmail);
            
            entity.HasOne(tm => tm.Organization)
                  .WithMany()
                  .HasForeignKey(tm => tm.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.Ignore(tm => tm.DomainEvents);
        });

        // InvitationToken (Secure tokens for team invitations)
        modelBuilder.Entity<InvitationToken>(entity =>
        {
            entity.ToTable("invitation_tokens", schema: "org");
            entity.HasKey(it => it.Id);
            entity.Property(it => it.TeamMemberId).IsRequired();
            entity.Property(it => it.Token).IsRequired().HasMaxLength(64);
            entity.Property(it => it.Email).IsRequired().HasMaxLength(200);
            entity.Property(it => it.OrganizationId).IsRequired();
            entity.Property(it => it.ExpiresAt).IsRequired();
            entity.Property(it => it.IsUsed).IsRequired();
            entity.Property(it => it.UsedAt);
            
            entity.HasIndex(it => it.Token).IsUnique();
            entity.HasIndex(it => it.TeamMemberId);
            entity.HasIndex(it => it.Email);
            entity.HasIndex(it => it.OrganizationId);
            
            entity.HasOne(it => it.TeamMember)
                  .WithMany()
                  .HasForeignKey(it => it.TeamMemberId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Invitation>(entity =>
        {
            entity.ToTable("invitations", schema: "org");
            entity.HasKey(i => i.Id);

            entity.Property(i => i.OrganizationId).IsRequired();
            entity.Property(i => i.Email).IsRequired().HasMaxLength(200);
            entity.Property(i => i.Role).IsRequired().HasMaxLength(50);
            entity.Property(i => i.Status).IsRequired();
            entity.Property(i => i.SecretHash).IsRequired().HasMaxLength(32);

            entity.Property(i => i.ExpiresAtUtc).IsRequired();
            entity.Property(i => i.CreatedAtUtc).IsRequired();
            entity.Property(i => i.AcceptedAtUtc);
            entity.Property(i => i.RevokedAtUtc);
            entity.Property(i => i.CreatedByUserId);

            entity.HasIndex(i => new { i.OrganizationId, i.Email, i.Status });
            entity.HasIndex(i => i.ExpiresAtUtc);

            entity.Ignore(i => i.DomainEvents);
        });

        modelBuilder.Entity<TenantOnboardingInvitation>(entity =>
        {
            entity.ToTable("tenant_onboarding_invitations", schema: "org");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.OrganizationId).IsRequired();
            entity.Property(x => x.Email).IsRequired().HasMaxLength(200);
            entity.Property(x => x.PropertyId);
            entity.Property(x => x.ExpiresAtUtc).IsRequired();
            entity.Property(x => x.UsedAtUtc);
            entity.Property(x => x.OccupantId);
            entity.Property(x => x.TokenHash).IsRequired().HasMaxLength(64);

            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.OrganizationId, x.Email });
            entity.HasIndex(x => x.ExpiresAtUtc);

            entity.Ignore(x => x.DomainEvents);
        });

        modelBuilder.Entity<IdempotencyRequestEntity>(b =>
        {
            b.ToTable("idempotency_requests", schema: "ops");
            b.HasKey(x => x.Id);

            b.Property(x => x.ClientId).HasColumnName("client_id").IsRequired();
            b.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").IsRequired();
            b.Property(x => x.RequestHash).HasColumnName("request_hash").IsRequired();

            b.Property(x => x.ResponseJson).HasColumnName("response_json").IsRequired();
            b.Property(x => x.ResponseBodyBase64).HasColumnName("response_body_base64").IsRequired();
            b.Property(x => x.ResponseContentType).HasColumnName("response_content_type").IsRequired();
            b.Property(x => x.StatusCode).HasColumnName("status_code").IsRequired();

            b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            b.Property(x => x.CompletedAtUtc).HasColumnName("completed_at_utc").IsRequired();

            b.HasIndex(x => new { x.ClientId, x.IdempotencyKey }).IsUnique();
        });

        // Property
        modelBuilder.Entity<Property>(entity =>
        {
            entity.ToTable("properties", schema: "locaguest");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.OrganizationId).IsRequired();
            entity.HasIndex(p => p.OrganizationId);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Address).IsRequired().HasMaxLength(300);
            entity.Property(p => p.City).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Rent).HasColumnType("decimal(18,2)");
            entity.Property(p => p.Surface).HasColumnType("decimal(18,2)");
            entity.Property(p => p.Charges).HasColumnType("decimal(18,2)");
            entity.Property(p => p.Deposit).HasColumnType("decimal(18,2)");

            entity.Property(p => p.Description).HasColumnType("text");
            entity.Property(p => p.PurchaseDate);

            entity.Property(p => p.EnergyClass).HasColumnType("text");
            entity.Property(p => p.ConstructionYear);

            entity.Property(p => p.PurchasePrice).HasColumnType("decimal(18,2)");
            entity.Property(p => p.Insurance).HasColumnType("decimal(18,2)");
            entity.Property(p => p.ManagementFeesRate).HasColumnType("decimal(18,2)");
            entity.Property(p => p.MaintenanceRate).HasColumnType("decimal(18,2)");
            entity.Property(p => p.VacancyRate).HasColumnType("decimal(18,2)");

            entity.Property(p => p.HasBalcony);

            entity.OwnsOne(p => p.Diagnostics, d =>
            {
                d.Property(x => x.DpeRating).HasColumnName("DpeRating");
                d.Property(x => x.DpeValue).HasColumnName("DpeValue");
                d.Property(x => x.GesRating).HasColumnName("GesRating");
                d.Property(x => x.ElectricDiagnosticDate).HasColumnName("ElectricDiagnosticDate");
                d.Property(x => x.ElectricDiagnosticExpiry).HasColumnName("ElectricDiagnosticExpiry");
                d.Property(x => x.GasDiagnosticDate).HasColumnName("GasDiagnosticDate");
                d.Property(x => x.GasDiagnosticExpiry).HasColumnName("GasDiagnosticExpiry");
                d.Property(x => x.HasAsbestos).HasColumnName("HasAsbestos");
                d.Property(x => x.AsbestosDiagnosticDate).HasColumnName("AsbestosDiagnosticDate");
                d.Property(x => x.ErpZone).HasColumnName("ErpZone");
            });

            entity.OwnsOne(p => p.AirbnbSettings, a =>
            {
                a.Property(x => x.MinimumStay).HasColumnName("MinimumStay");
                a.Property(x => x.MaximumStay).HasColumnName("MaximumStay");
                a.Property(x => x.PricePerNight).HasColumnName("PricePerNight");
                a.Property(x => x.NightsBookedPerMonth).HasColumnName("NightsBookedPerMonth");
            });
            
            // Store list as JSON (not JSONB for simplicity)
            entity.Property(p => p.ImageUrls)
                .HasConversion(
                    v => string.Join(",", v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                );

            entity.Property(p => p.ImageUrls).Metadata.SetValueComparer(stringListComparer);
            
            entity.Ignore(p => p.DomainEvents);
            
            // Configure relationship with PropertyRooms
            entity.HasMany(p => p.Rooms)
                .WithOne()
                .HasForeignKey(r => r.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PropertyImage>(entity =>
        {
            entity.ToTable("property_images", schema: "locaguest");
            entity.HasKey(pi => pi.Id);

            entity.Property(pi => pi.OrganizationId).IsRequired();
            entity.HasIndex(pi => pi.OrganizationId);
            entity.HasIndex(pi => new { pi.OrganizationId, pi.PropertyId });

            entity.Property(pi => pi.PropertyId).IsRequired();
            entity.HasIndex(pi => pi.PropertyId);
        });
        
        // PropertyRoom (Chambres de colocation)
        modelBuilder.Entity<PropertyRoom>(entity =>
        {
            entity.ToTable("property_rooms", schema: "locaguest");
            entity.HasKey(r => r.Id);

            entity.Property(r => r.OrganizationId).IsRequired();
            entity.HasIndex(r => r.OrganizationId);
            entity.HasIndex(r => new { r.OrganizationId, r.PropertyId });

            entity.Property(r => r.PropertyId).IsRequired();
            entity.HasIndex(r => r.PropertyId);
            entity.Property(r => r.Name).IsRequired().HasMaxLength(100);
            entity.Property(r => r.Rent).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(r => r.Surface).HasColumnType("decimal(18,2)");
            entity.Property(r => r.Charges).HasColumnType("decimal(18,2)");
            entity.Property(r => r.Description).HasMaxLength(500);
            entity.Property(r => r.Status).IsRequired();
            entity.Property(r => r.CurrentContractId);
            entity.HasIndex(r => r.CurrentContractId);
            entity.Property(r => r.OnHoldUntilUtc);
            entity.HasIndex(r => r.OnHoldUntilUtc);
            
            entity.Ignore(r => r.DomainEvents);
        });

        // Tenant (Locataire - à ne pas confondre avec TenantId multi-tenant)
        modelBuilder.Entity<Occupant>(entity =>
        {
            entity.ToTable("occupants", schema: "locaguest");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.OrganizationId).IsRequired();
            entity.HasIndex(t => t.OrganizationId);
            entity.Property(t => t.FullName).IsRequired().HasMaxLength(200);
            entity.Property(t => t.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(t => new { t.OrganizationId, t.Email }).IsUnique();
            entity.Property(t => t.Code).IsRequired().HasMaxLength(50);
            entity.HasIndex(t => new { t.OrganizationId, t.Code }).IsUnique();
            entity.Property(t => t.Phone).HasMaxLength(50);
            entity.Ignore(t => t.DomainEvents);
        });

        // Contract
        modelBuilder.Entity<Contract>(entity =>
        {
            entity.ToTable("contracts", schema: "lease");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.OrganizationId).IsRequired();
            entity.HasIndex(c => c.OrganizationId);
            entity.Property(c => c.Code).IsRequired().HasMaxLength(50);
            entity.HasIndex(c => new { c.OrganizationId, c.Code }).IsUnique();
            entity.Property(c => c.RenterOccupantId).IsRequired();
            entity.HasIndex(c => c.RenterOccupantId);
            entity.Property(c => c.Rent).HasColumnType("decimal(18,2)");
            entity.Property(c => c.Deposit).HasColumnType("decimal(18,2)");

            entity.Property(c => c.TerminationDate);
            entity.Property(c => c.TerminationReason).HasColumnType("text");

            entity.Property(c => c.NoticeDate);
            entity.Property(c => c.NoticeEndDate);
            entity.Property(c => c.NoticeReason).HasColumnType("text");

            // Configure RequiredDocuments as owned entity collection
            entity.OwnsMany(c => c.RequiredDocuments, rd =>
            {
                rd.ToTable("required_documents", schema: "lease");
                rd.WithOwner().HasForeignKey("ContractId");
                rd.Property<Guid>("ContractId");
                rd.HasKey("ContractId", "Type");
                rd.Property(r => r.Type).HasConversion<string>().HasMaxLength(50);
                rd.Property(r => r.IsRequired);
                rd.Property(r => r.IsProvided);
                rd.Property(r => r.IsSigned);
            });
            entity.Ignore(c => c.DomainEvents);

            entity.HasOne<Property>()
                .WithMany()
                .HasForeignKey(c => c.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Occupant>()
                .WithMany()
                .HasForeignKey(c => c.RenterOccupantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // UserSettings
        modelBuilder.Entity<UserSettings>(entity =>
        {
            entity.ToTable("user_settings", schema: "iam");
            entity.HasKey(us => us.Id);
            entity.Property(us => us.OrganizationId).IsRequired();
            entity.HasIndex(us => new { us.OrganizationId, us.UserId }).IsUnique();
            entity.Property(us => us.Language).IsRequired().HasMaxLength(10);
            entity.Property(us => us.Timezone).IsRequired().HasMaxLength(100);
            entity.Property(us => us.DateFormat).IsRequired().HasMaxLength(20);
            entity.Property(us => us.Currency).IsRequired().HasMaxLength(10);
            entity.Property(us => us.PhotoUrl).HasMaxLength(500);
            entity.Ignore(us => us.DomainEvents);
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("user_profiles", schema: "iam");
            entity.HasKey(up => up.Id);
            entity.Property(up => up.OrganizationId).IsRequired();
            entity.HasIndex(up => new { up.OrganizationId, up.UserId }).IsUnique();
            entity.Property(up => up.UserId).IsRequired().HasMaxLength(100);
            entity.Property(up => up.FirstName).IsRequired().HasMaxLength(200);
            entity.Property(up => up.LastName).IsRequired().HasMaxLength(200);
            entity.Property(up => up.Email).IsRequired().HasMaxLength(200);
            entity.Property(up => up.Phone).HasMaxLength(50);
            entity.Property(up => up.Company).HasMaxLength(200);
            entity.Property(up => up.Role).HasMaxLength(50);
            entity.Property(up => up.Bio).HasMaxLength(2000);
            entity.Property(up => up.PhotoUrl).HasMaxLength(500);
            entity.Ignore(up => up.DomainEvents);
        });

        modelBuilder.Entity<UserPreferences>(entity =>
        {
            entity.ToTable("user_preferences", schema: "iam");
            entity.HasKey(up => up.Id);
            entity.Property(up => up.OrganizationId).IsRequired();
            entity.HasIndex(up => new { up.OrganizationId, up.UserId }).IsUnique();
            entity.Property(up => up.UserId).IsRequired().HasMaxLength(100);
            entity.Property(up => up.Language).IsRequired().HasMaxLength(10);
            entity.Property(up => up.Timezone).IsRequired().HasMaxLength(100);
            entity.Property(up => up.DateFormat).IsRequired().HasMaxLength(20);
            entity.Property(up => up.Currency).IsRequired().HasMaxLength(10);
            entity.Ignore(up => up.DomainEvents);
        });

        modelBuilder.Entity<NotificationSettings>(entity =>
        {
            entity.ToTable("notification_settings", schema: "iam");
            entity.HasKey(ns => ns.Id);
            entity.Property(ns => ns.OrganizationId).IsRequired();
            entity.HasIndex(ns => new { ns.OrganizationId, ns.UserId }).IsUnique();
            entity.Property(ns => ns.UserId).IsRequired().HasMaxLength(100);
            entity.Ignore(ns => ns.DomainEvents);
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.ToTable("user_sessions", schema: "iam");
            entity.HasKey(us => us.Id);
            entity.Property(us => us.UserId).IsRequired().HasMaxLength(100);
        });

        // RentabilityScenario
        modelBuilder.Entity<RentabilityScenario>(entity =>
        {
            entity.ToTable("rentability_scenarios", schema: "analytics");
            entity.HasKey(rs => rs.Id);
            entity.Property(rs => rs.OrganizationId).IsRequired();
            entity.HasIndex(rs => rs.OrganizationId);
            entity.Property(rs => rs.Name).IsRequired().HasMaxLength(200);
            entity.Property(rs => rs.PropertyType).IsRequired().HasMaxLength(50);
            entity.Property(rs => rs.Location).IsRequired().HasMaxLength(300);
            entity.Property(rs => rs.State).IsRequired().HasMaxLength(50);
            entity.Property(rs => rs.Strategy).IsRequired().HasMaxLength(50);
            entity.Property(rs => rs.Objective).IsRequired().HasMaxLength(50);
            entity.Property(rs => rs.Indexation).IsRequired().HasMaxLength(20);
            entity.Property(rs => rs.LoanType).IsRequired().HasMaxLength(20);
            entity.Property(rs => rs.DeferredType).IsRequired().HasMaxLength(20);
            entity.Property(rs => rs.TaxRegime).IsRequired().HasMaxLength(20);
            entity.Property(rs => rs.ExitMethod).IsRequired().HasMaxLength(50);
            
            // Decimal precision
            entity.Property(rs => rs.Surface).HasColumnType("decimal(10,2)");
            entity.Property(rs => rs.PurchasePrice).HasColumnType("decimal(18,2)");
            entity.Property(rs => rs.NotaryFees).HasColumnType("decimal(18,2)");
            entity.Property(rs => rs.RenovationCost).HasColumnType("decimal(18,2)");
            entity.Property(rs => rs.LandValue).HasColumnType("decimal(18,2)");
            entity.Property(rs => rs.FurnitureCost).HasColumnType("decimal(18,2)");
            entity.Property(rs => rs.MonthlyRent).HasColumnType("decimal(18,2)");
            entity.Property(rs => rs.IndexationRate).HasColumnType("decimal(5,2)");
            entity.Property(rs => rs.VacancyRate).HasColumnType("decimal(5,2)");
            entity.Property(rs => rs.HighSeasonMultiplier).HasColumnType("decimal(5,2)");
            entity.Property(rs => rs.ParkingRent).HasColumnType("decimal(18,2)");
            entity.Property(rs => rs.StorageRent).HasColumnType("decimal(18,2)");
            entity.Property(rs => rs.OtherRevenues).HasColumnType("decimal(18,2)");
            entity.Property(rs => rs.RelocationIncrease).HasColumnType("decimal(5,2)");
            entity.Property(rs => rs.CondoFees).HasColumnType("decimal(18,2)");
            entity.Property(rs => rs.Insurance).HasColumnType("decimal(18,2)");
            entity.Property(rs => rs.PropertyTax).HasColumnType("decimal(18,2)");
            entity.Property(rs => rs.ManagementFees).HasColumnType("decimal(5,2)");
            entity.Property(rs => rs.MaintenanceRate).HasColumnType("decimal(5,2)");
            entity.Property(rs => rs.RecoverableCharges).HasColumnType("decimal(18,2)");
            entity.Property(rs => rs.ChargesIncrease).HasColumnType("decimal(5,2)");
            entity.Property(rs => rs.LoanAmount).HasColumnType("decimal(18,2)");
            entity.Property(rs => rs.InterestRate).HasColumnType("decimal(5,2)");
            entity.Property(rs => rs.InsuranceRate).HasColumnType("decimal(5,2)");
            entity.Property(rs => rs.EarlyRepaymentPenalty).HasColumnType("decimal(5,2)");
            entity.Property(rs => rs.MarginalTaxRate).HasColumnType("decimal(5,2)");
            entity.Property(rs => rs.SocialContributions).HasColumnType("decimal(5,2)");
            entity.Property(rs => rs.TargetCapRate).HasColumnType("decimal(5,2)");
            entity.Property(rs => rs.AnnualAppreciation).HasColumnType("decimal(5,2)");
            entity.Property(rs => rs.TargetPricePerSqm).HasColumnType("decimal(18,2)");
            entity.Property(rs => rs.SellingCosts).HasColumnType("decimal(5,2)");
            entity.Property(rs => rs.CapitalGainsTax).HasColumnType("decimal(5,2)");
            
            // JSON columns
            entity.Property(rs => rs.PlannedCapexJson).HasColumnType("text");
            entity.Property(rs => rs.ResultsJson).HasColumnType("text");
            
            entity.HasIndex(rs => new { rs.OrganizationId, rs.UserId });
            entity.HasIndex(rs => new { rs.OrganizationId, rs.UserId, rs.IsBase });
            
            entity.HasMany(rs => rs.Versions)
                .WithOne()
                .HasForeignKey(v => v.ScenarioId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasMany(rs => rs.Shares)
                .WithOne()
                .HasForeignKey(s => s.ScenarioId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.Ignore(rs => rs.DomainEvents);
        });

        // ScenarioVersion
        modelBuilder.Entity<ScenarioVersion>(entity =>
        {
            entity.ToTable("scenario_versions", schema: "analytics");
            entity.HasKey(sv => sv.Id);
            entity.Property(sv => sv.ChangeDescription).IsRequired().HasMaxLength(500);
            entity.Property(sv => sv.SnapshotJson).IsRequired().HasColumnType("text");
            entity.HasIndex(sv => sv.ScenarioId);
            entity.Ignore(sv => sv.DomainEvents);
        });

        // ScenarioShare
        modelBuilder.Entity<ScenarioShare>(entity =>
        {
            entity.ToTable("scenario_shares", schema: "analytics");
            entity.HasKey(ss => ss.Id);
            entity.Property(ss => ss.Permission).IsRequired().HasMaxLength(20);
            entity.HasIndex(ss => ss.ScenarioId);
            entity.HasIndex(ss => ss.SharedWithUserId);
            entity.HasIndex(ss => new { ss.ScenarioId, ss.SharedWithUserId });
            entity.Ignore(ss => ss.DomainEvents);
        });
        
        // Plan (configuration globale, pas de TenantId)
        modelBuilder.Entity<Plan>(entity =>
        {
            entity.ToTable("plans", schema: "billing");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Code).IsRequired().HasMaxLength(50);
            entity.HasIndex(p => p.Code).IsUnique();
            entity.Ignore(p => p.DomainEvents);
        });
        
        // Subscription (par utilisateur/tenant)
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.ToTable("subscriptions", schema: "billing");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.OrganizationId).IsRequired();
            entity.HasIndex(s => new { s.OrganizationId, s.UserId });
            entity.HasIndex(s => s.StripeSubscriptionId);
            entity.Ignore(s => s.DomainEvents);
        });
        
        // UsageEvent
        modelBuilder.Entity<UsageEvent>(entity =>
        {
            entity.ToTable("usage_events", schema: "billing");
            entity.HasKey(ue => ue.Id);
            entity.Property(ue => ue.OrganizationId).IsRequired();
            entity.HasIndex(ue => new { ue.OrganizationId, ue.SubscriptionId });
            entity.Ignore(ue => ue.DomainEvents);
        });

        // UsageAggregate
        modelBuilder.Entity<UsageAggregate>(entity =>
        {
            entity.ToTable("usage_aggregates", schema: "billing");
            entity.HasKey(ua => ua.Id);
            entity.Property(ua => ua.OrganizationId).IsRequired();
            entity.HasIndex(ua => new { ua.OrganizationId, ua.UserId, ua.Dimension, ua.PeriodYear, ua.PeriodMonth }).IsUnique();
            entity.Ignore(ua => ua.DomainEvents);
        });
        
        // TrackingEvent (Analytics)
        modelBuilder.Entity<TrackingEvent>(entity =>
        {
            entity.ToTable("tracking_events", schema: "analytics");
            entity.HasKey(te => te.Id);
            
            entity.Property(te => te.OrganizationId).IsRequired();
            entity.Property(te => te.UserId).IsRequired();
            entity.Property(te => te.EventType).IsRequired().HasMaxLength(100);
            entity.Property(te => te.PageName).HasMaxLength(200);
            entity.Property(te => te.Url).HasMaxLength(500);
            entity.Property(te => te.UserAgent).IsRequired().HasMaxLength(500);
            entity.Property(te => te.IpAddress).IsRequired().HasMaxLength(45);
            entity.Property(te => te.SessionId).HasMaxLength(100);
            entity.Property(te => te.CorrelationId).HasMaxLength(100);
            
            // Metadata as JSONB for PostgreSQL
            entity.Property(te => te.Metadata).HasColumnType("jsonb");
            
            // Indexes for performance
            entity.HasIndex(te => te.OrganizationId);
            entity.HasIndex(te => te.UserId);
            entity.HasIndex(te => te.EventType);
            entity.HasIndex(te => te.Timestamp);
            entity.HasIndex(te => new { te.OrganizationId, te.Timestamp });
            entity.HasIndex(te => new { te.OrganizationId, te.UserId, te.Timestamp });
            entity.HasIndex(te => new { te.EventType, te.Timestamp });
        });
        
        // ✅ InventoryEntry (EDL Entrée)
        modelBuilder.Entity<InventoryEntry>(entity =>
        {
            entity.ToTable("inventory_entries", schema: "inventory");
            entity.HasKey(ie => ie.Id);
            
            // TenantId (string) hérité de AuditableEntity pour multi-tenant
            entity.Property(ie => ie.OrganizationId).IsRequired();
            entity.HasIndex(ie => ie.OrganizationId);
            
            // RenterOccupantId (Guid) pour le locataire du contrat
            entity.Property(ie => ie.RenterOccupantId).IsRequired().HasColumnName("RenterOccupantId");
            entity.HasIndex(ie => ie.RenterOccupantId);
            
            entity.Property(ie => ie.PropertyId).IsRequired();
            entity.Property(ie => ie.ContractId).IsRequired();
            entity.HasIndex(ie => ie.ContractId);
            entity.Property(ie => ie.AgentName).IsRequired().HasMaxLength(200);
            entity.Property(ie => ie.InspectionDate).IsRequired();
            entity.Property(ie => ie.Status).IsRequired();
            entity.Property(ie => ie.GeneralObservations).HasMaxLength(1000);
            entity.Property(ie => ie.RepresentativeName).HasMaxLength(200);
            
            // PhotoUrls as comma-separated string
            entity.Property(ie => ie.PhotoUrls)
                .HasConversion(
                    v => string.Join(",", v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                );

            entity.Property(ie => ie.PhotoUrls).Metadata.SetValueComparer(stringListComparer);
            
            // Items as owned entities
            entity.OwnsMany(ie => ie.Items, item =>
            {
                item.ToTable("inventory_items", schema: "inventory");
                item.WithOwner().HasForeignKey("InventoryEntryId");
                item.Property<Guid>("InventoryEntryId");
                item.HasKey("InventoryEntryId", "RoomName", "ElementName");
                item.Property(i => i.RoomName).IsRequired().HasMaxLength(100);
                item.Property(i => i.ElementName).IsRequired().HasMaxLength(100);
                item.Property(i => i.Category).IsRequired().HasMaxLength(50);
                item.Property(i => i.Condition).IsRequired();
                item.Property(i => i.Comment).HasMaxLength(500);
                item.Property(i => i.PhotoUrls)
                    .HasConversion(
                        v => string.Join(",", v),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                    );

                item.Property(i => i.PhotoUrls).Metadata.SetValueComparer(stringListComparer);
            });
            
            entity.Ignore(ie => ie.DomainEvents);
        });
        
        // ✅ InventoryExit (EDL Sortie)
        modelBuilder.Entity<InventoryExit>(entity =>
        {
            entity.ToTable("inventory_exits", schema: "inventory");
            entity.HasKey(ie => ie.Id);
            
            // TenantId (string) hérité de AuditableEntity pour multi-tenant
            entity.Property(ie => ie.OrganizationId).IsRequired();
            entity.HasIndex(ie => ie.OrganizationId);
            
            // RenterOccupantId (Guid) pour le locataire du contrat
            entity.Property(ie => ie.RenterOccupantId).IsRequired().HasColumnName("RenterOccupantId");
            entity.HasIndex(ie => ie.RenterOccupantId);
            
            entity.Property(ie => ie.PropertyId).IsRequired();
            entity.Property(ie => ie.ContractId).IsRequired();
            entity.HasIndex(ie => ie.ContractId);
            entity.Property(ie => ie.InventoryEntryId).IsRequired();
            entity.HasIndex(ie => ie.InventoryEntryId);
            entity.Property(ie => ie.AgentName).IsRequired().HasMaxLength(200);
            entity.Property(ie => ie.InspectionDate).IsRequired();
            entity.Property(ie => ie.TotalDeductionAmount).HasColumnType("decimal(18,2)");
            entity.Property(ie => ie.OwnerCoveredAmount).HasColumnType("decimal(18,2)");
            entity.Property(ie => ie.FinancialNotes).HasMaxLength(1000);
            entity.Property(ie => ie.Status).IsRequired();
            entity.Property(ie => ie.IsFinalized).IsRequired();
            entity.Property(ie => ie.FinalizedAt);
            entity.Property(ie => ie.GeneralObservations).HasMaxLength(1000);
            entity.Property(ie => ie.RepresentativeName).HasMaxLength(200);
            
            entity.Property(ie => ie.PhotoUrls)
                .HasConversion(
                    v => string.Join(",", v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                );
            
            // Comparisons as owned entities
            entity.OwnsMany(ie => ie.Comparisons, comp =>
            {
                comp.ToTable("inventory_comparisons", schema: "inventory");
                comp.WithOwner().HasForeignKey("InventoryExitId");
                comp.Property<Guid>("InventoryExitId");
                comp.HasKey("InventoryExitId", "RoomName", "ElementName");
                comp.Property(c => c.RoomName).IsRequired().HasMaxLength(100);
                comp.Property(c => c.ElementName).IsRequired().HasMaxLength(100);
                comp.Property(c => c.EntryCondition).IsRequired();
                comp.Property(c => c.ExitCondition).IsRequired();
                comp.Property(c => c.Comment).HasMaxLength(500);
                comp.Property(c => c.PhotoUrls)
                    .HasConversion(
                        v => string.Join(",", v),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                    );

                comp.Property(c => c.PhotoUrls).Metadata.SetValueComparer(stringListComparer);
            });
            
            // Degradations as owned entities
            entity.OwnsMany(ie => ie.Degradations, deg =>
            {
                deg.ToTable("inventory_degradations", schema: "inventory");
                deg.WithOwner().HasForeignKey("InventoryExitId");
                deg.Property<Guid>("InventoryExitId");
                deg.HasKey("InventoryExitId", "RoomName", "ElementName");
                deg.Property(d => d.RoomName).IsRequired().HasMaxLength(100);
                deg.Property(d => d.ElementName).IsRequired().HasMaxLength(100);
                deg.Property(d => d.Description).IsRequired().HasMaxLength(500);
                deg.Property(d => d.EstimatedCost).HasColumnType("decimal(18,2)");
                deg.Property(d => d.IsImputedToTenant).IsRequired();
                deg.Property(d => d.PhotoUrls)
                    .HasConversion(
                        v => string.Join(",", v),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                    );

                deg.Property(d => d.PhotoUrls).Metadata.SetValueComparer(stringListComparer);
            });
            
            entity.Ignore(ie => ie.DomainEvents);
        });

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LocaGuest.Infrastructure.Persistence.Configurations.PaymentConfiguration).Assembly);
    }
    
    /// <summary>
    /// Configure les Global Query Filters pour l'isolation multi-tenant
    /// Toutes les requêtes seront automatiquement filtrées par TenantId
    /// </summary>
    private void ConfigureMultiTenantFilters(ModelBuilder modelBuilder)
    {
        var entityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(t => t.ClrType != null)
            .Where(t => !t.IsOwned());

        foreach (var entityType in entityTypes)
        {
            var orgIdProperty = entityType.FindProperty("OrganizationId");
            if (orgIdProperty == null || orgIdProperty.ClrType != typeof(Guid))
                continue;

            var method = typeof(LocaGuestDbContext)
                .GetMethod(nameof(ApplyOrganizationFilter), BindingFlags.NonPublic | BindingFlags.Instance);

            method?.MakeGenericMethod(entityType.ClrType!).Invoke(this, new object[] { modelBuilder });
        }
    }
    
    /// <summary>
    /// Applique un filtre global sur une entité pour filtrer par TenantId
    /// </summary>
    private void ApplyOrganizationFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class
    {
        // IMPORTANT: Ne PAS capturer OrganizationId dans une variable locale
        // car cela fige la valeur au moment de OnModelCreating (avant l'authentification).
        // Le filtre doit évaluer _orgContext à CHAQUE requête.
        modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
            (_orgContext != null && _orgContext.IsSystemContext && _orgContext.CanBypassOrganizationFilter)
            ||
            (_orgContext != null
             && _orgContext.OrganizationId.HasValue
             && EF.Property<Guid>(e, "OrganizationId") == _orgContext.OrganizationId.GetValueOrDefault()));
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Audit et isolation multi-tenant
        var entries = ChangeTracker.Entries<AuditableEntity>();

        var userId = _currentUserService?.UserId?.ToString() ?? "system";
        var now = DateTime.UtcNow;

        var orgId = _orgContext?.OrganizationId;
        var isAuthenticated = _orgContext?.IsAuthenticated == true;
        var bypass = _orgContext?.IsSystemContext == true && _orgContext?.CanBypassOrganizationFilter == true;

        if (isAuthenticated && !bypass && !orgId.HasValue && _orgContext?.IsSystemContext != true)
        {
            throw new UnauthorizedAccessException("Authenticated request is missing OrganizationId.");
        }

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                if (!bypass)
                {
                    if (orgId.HasValue)
                    {
                        entry.Entity.SetOrganizationId(orgId.Value);
                    }
                    else
                    {
                        // Contexte non authentifié (jobs, tâches internes, tests...)
                        // => exiger que l'entité ait déjà été taggée explicitement.
                        if (entry.Entity.OrganizationId == Guid.Empty)
                            throw new UnauthorizedAccessException("Missing OrganizationId for created entity.");
                    }
                }
                else
                {
                    if (entry.Entity.OrganizationId == Guid.Empty)
                        throw new InvalidOperationException("System context must explicitly set OrganizationId on created entities.");
                }

                entry.Entity.CreatedBy = userId;
                entry.Entity.CreatedAt = now;
            }

            if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                var orgIdProp = entry.Property(nameof(AuditableEntity.OrganizationId));
                if (orgIdProp.IsModified && !Equals(orgIdProp.OriginalValue, orgIdProp.CurrentValue))
                    throw new InvalidOperationException("OrganizationId cannot be modified after entity creation.");

                if (!bypass)
                {
                    if (entry.Entity.OrganizationId == Guid.Empty)
                        throw new UnauthorizedAccessException("Entity without OrganizationId cannot be modified.");

                    if (entry.Entity.OrganizationId != orgId!.Value)
                        throw new UnauthorizedAccessException("Cross-organization data access is forbidden.");
                }

                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.LastModifiedBy = userId;
                    entry.Entity.LastModifiedAt = now;
                }
            }
        }

        // Sauvegarder
        var result = await base.SaveChangesAsync(cancellationToken);

        // Dispatcher les Domain Events après persistence
        await DispatchDomainEventsAsync(cancellationToken);

        return result;
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        var domainEntities = ChangeTracker.Entries<Entity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        domainEntities.ForEach(e => e.Entity.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
    }
}

