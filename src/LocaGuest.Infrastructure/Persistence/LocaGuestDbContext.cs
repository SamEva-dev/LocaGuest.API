using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Aggregates.DocumentAggregate;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Aggregates.TenantAggregate;
using LocaGuest.Domain.Aggregates.UserAggregate;
using LocaGuest.Domain.Aggregates.SubscriptionAggregate;
using LocaGuest.Domain.Aggregates.RentabilityAggregate;
using LocaGuest.Domain.Aggregates.OrganizationAggregate;
using LocaGuest.Domain.Aggregates.InventoryAggregate;
using LocaGuest.Domain.Aggregates.PaymentAggregate;
using LocaGuest.Domain.Entities;
using LocaGuest.Domain.Analytics;
using LocaGuest.Domain.Common;
using LocaGuest.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using LocaGuest.Application.Services;

namespace LocaGuest.Infrastructure.Persistence;

public class LocaGuestDbContext : DbContext, ILocaGuestDbContext
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService? _currentUserService;
    private readonly ITenantContext? _tenantContext;

    public LocaGuestDbContext(
        DbContextOptions<LocaGuestDbContext> options,
        IMediator mediator,
        ICurrentUserService? currentUserService = null,
        ITenantContext? tenantContext = null)
        : base(options)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
        _tenantContext = tenantContext;
    }

    // Multi-tenant Organizations
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<TenantSequence> TenantSequences => Set<TenantSequence>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<InvitationToken> InvitationTokens => Set<InvitationToken>();

    public DbSet<Property> Properties => Set<Property>();
    public DbSet<PropertyRoom> PropertyRooms => Set<PropertyRoom>();
    public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<Addendum> Addendums => Set<Addendum>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Domain.Aggregates.PaymentAggregate.Payment> Payments => Set<Domain.Aggregates.PaymentAggregate.Payment>();
    public DbSet<RentInvoice> RentInvoices => Set<RentInvoice>();
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuration Global Query Filters pour isolation multi-tenant
        ConfigureMultiTenantFilters(modelBuilder);

        // Organization (Multi-tenant root entity)
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.ToTable("organizations");
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

        // TenantSequence (Numbering service per organization)
        modelBuilder.Entity<TenantSequence>(entity =>
        {
            entity.ToTable("tenant_sequences");
            entity.HasKey(ts => new { ts.TenantId, ts.EntityPrefix });
            entity.Property(ts => ts.TenantId).IsRequired();
            entity.Property(ts => ts.EntityPrefix).IsRequired().HasMaxLength(10);
            entity.Property(ts => ts.LastNumber).IsRequired();
            entity.Property(ts => ts.Description).HasMaxLength(200);
            entity.HasIndex(ts => ts.TenantId);
        });

        // TeamMember (Members of an organization)
        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.ToTable("team_members");
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
            entity.ToTable("invitation_tokens");
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

        // Property
        modelBuilder.Entity<Property>(entity =>
        {
            entity.ToTable("properties");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.TenantId).IsRequired().HasMaxLength(100);
            entity.HasIndex(p => p.TenantId);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Address).IsRequired().HasMaxLength(300);
            entity.Property(p => p.City).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Rent).HasColumnType("decimal(18,2)");
            entity.Property(p => p.Surface).HasColumnType("decimal(18,2)");
            entity.Property(p => p.Charges).HasColumnType("decimal(18,2)");
            entity.Property(p => p.Deposit).HasColumnType("decimal(18,2)");
            
            // Store list as JSON (not JSONB for simplicity)
            entity.Property(p => p.ImageUrls)
                .HasConversion(
                    v => string.Join(",", v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                );
            
            entity.Ignore(p => p.DomainEvents);
            
            // Configure relationship with PropertyRooms
            entity.HasMany(p => p.Rooms)
                .WithOne()
                .HasForeignKey(r => r.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // PropertyRoom (Chambres de colocation)
        modelBuilder.Entity<PropertyRoom>(entity =>
        {
            entity.ToTable("property_rooms");
            entity.HasKey(r => r.Id);
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
            
            entity.Ignore(r => r.DomainEvents);
        });

        // Tenant (Locataire - à ne pas confondre avec TenantId multi-tenant)
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("tenants");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.TenantId).IsRequired().HasMaxLength(100);
            entity.HasIndex(t => t.TenantId);
            entity.Property(t => t.FullName).IsRequired().HasMaxLength(200);
            entity.Property(t => t.Email).IsRequired().HasMaxLength(200);
            entity.Property(t => t.Phone).HasMaxLength(50);
            entity.Ignore(t => t.DomainEvents);
        });

        // Contract
        modelBuilder.Entity<Contract>(entity =>
        {
            entity.ToTable("contracts");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.TenantId).IsRequired().HasMaxLength(100);
            entity.HasIndex(c => c.TenantId);
            entity.Property(c => c.RenterTenantId).IsRequired();
            entity.HasIndex(c => c.RenterTenantId);
            entity.Property(c => c.Rent).HasColumnType("decimal(18,2)");
            entity.Property(c => c.Deposit).HasColumnType("decimal(18,2)");
            
            entity.HasMany(c => c.Payments)
                  .WithOne()
                  .HasForeignKey(p => p.ContractId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Configure RequiredDocuments as owned entity collection
            entity.OwnsMany(c => c.RequiredDocuments, rd =>
            {
                rd.ToTable("contract_required_documents");
                rd.WithOwner().HasForeignKey("ContractId");
                rd.Property<Guid>("ContractId");
                rd.HasKey("ContractId", "Type");
                rd.Property(r => r.Type).HasConversion<string>().HasMaxLength(50);
                rd.Property(r => r.IsRequired);
                rd.Property(r => r.IsProvided);
                rd.Property(r => r.IsSigned);
            });

            // Configure DocumentIds as a JSON column or ignore for now
            entity.Ignore(c => c.DocumentIds);
            entity.Ignore(c => c.DomainEvents);
        });

        // ContractPayment (old/deprecated - use PaymentAggregate.Payment instead)
        modelBuilder.Entity<ContractPayment>(entity =>
        {
            entity.ToTable("contract_payments");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Amount).HasColumnType("decimal(18,2)");
            entity.Ignore(p => p.DomainEvents);
        });

        // UserSettings
        modelBuilder.Entity<UserSettings>(entity =>
        {
            entity.ToTable("user_settings");
            entity.HasKey(us => us.Id);
            entity.Property(us => us.TenantId).IsRequired().HasMaxLength(100);
            entity.HasIndex(us => new { us.TenantId, us.UserId }).IsUnique();
            entity.Property(us => us.Language).IsRequired().HasMaxLength(10);
            entity.Property(us => us.Timezone).IsRequired().HasMaxLength(100);
            entity.Property(us => us.DateFormat).IsRequired().HasMaxLength(20);
            entity.Property(us => us.Currency).IsRequired().HasMaxLength(10);
            entity.Property(us => us.PhotoUrl).HasMaxLength(500);
            entity.Ignore(us => us.DomainEvents);
        });

        // RentabilityScenario
        modelBuilder.Entity<RentabilityScenario>(entity =>
        {
            entity.ToTable("rentability_scenarios");
            entity.HasKey(rs => rs.Id);
            entity.Property(rs => rs.TenantId).IsRequired().HasMaxLength(100);
            entity.HasIndex(rs => rs.TenantId);
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
            
            entity.HasIndex(rs => new { rs.TenantId, rs.UserId });
            entity.HasIndex(rs => new { rs.TenantId, rs.UserId, rs.IsBase });
            
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
            entity.ToTable("scenario_versions");
            entity.HasKey(sv => sv.Id);
            entity.Property(sv => sv.ChangeDescription).IsRequired().HasMaxLength(500);
            entity.Property(sv => sv.SnapshotJson).IsRequired().HasColumnType("text");
            entity.HasIndex(sv => sv.ScenarioId);
            entity.Ignore(sv => sv.DomainEvents);
        });

        // ScenarioShare
        modelBuilder.Entity<ScenarioShare>(entity =>
        {
            entity.ToTable("scenario_shares");
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
            entity.ToTable("plans");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Code).IsRequired().HasMaxLength(50);
            entity.HasIndex(p => p.Code).IsUnique();
            entity.Ignore(p => p.DomainEvents);
        });
        
        // Subscription (par utilisateur/tenant)
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.ToTable("subscriptions");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.TenantId).IsRequired().HasMaxLength(100);
            entity.HasIndex(s => new { s.TenantId, s.UserId });
            entity.HasIndex(s => s.StripeSubscriptionId);
            entity.Ignore(s => s.DomainEvents);
        });
        
        // UsageEvent
        modelBuilder.Entity<UsageEvent>(entity =>
        {
            entity.ToTable("usage_events");
            entity.HasKey(ue => ue.Id);
            entity.Property(ue => ue.TenantId).IsRequired().HasMaxLength(100);
            entity.HasIndex(ue => new { ue.TenantId, ue.SubscriptionId });
            entity.Ignore(ue => ue.DomainEvents);
        });
        
        // UsageAggregate
        modelBuilder.Entity<UsageAggregate>(entity =>
        {
            entity.ToTable("usage_aggregates");
            entity.HasKey(ua => ua.Id);
            entity.Property(ua => ua.TenantId).IsRequired().HasMaxLength(100);
            entity.HasIndex(ua => new { ua.TenantId, ua.UserId, ua.Dimension, ua.PeriodYear, ua.PeriodMonth }).IsUnique();
            entity.Ignore(ua => ua.DomainEvents);
        });
        
        // TrackingEvent (Analytics)
        modelBuilder.Entity<TrackingEvent>(entity =>
        {
            entity.ToTable("tracking_events");
            entity.HasKey(te => te.Id);
            
            entity.Property(te => te.TenantId).IsRequired();
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
            entity.HasIndex(te => te.TenantId);
            entity.HasIndex(te => te.UserId);
            entity.HasIndex(te => te.EventType);
            entity.HasIndex(te => te.Timestamp);
            entity.HasIndex(te => new { te.TenantId, te.Timestamp });
            entity.HasIndex(te => new { te.TenantId, te.UserId, te.Timestamp });
            entity.HasIndex(te => new { te.EventType, te.Timestamp });
        });
        
        // ✅ InventoryEntry (EDL Entrée)
        modelBuilder.Entity<InventoryEntry>(entity =>
        {
            entity.ToTable("inventory_entries");
            entity.HasKey(ie => ie.Id);
            
            // TenantId (string) hérité de AuditableEntity pour multi-tenant
            entity.Property(ie => ie.TenantId).IsRequired().HasMaxLength(100);
            entity.HasIndex(ie => ie.TenantId);
            
            // RenterTenantId (Guid) pour le locataire du contrat
            entity.Property(ie => ie.RenterTenantId).IsRequired().HasColumnName("RenterTenantId");
            entity.HasIndex(ie => ie.RenterTenantId);
            
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
            
            // Items as owned entities
            entity.OwnsMany(ie => ie.Items, item =>
            {
                item.ToTable("inventory_items");
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
            });
            
            entity.Ignore(ie => ie.DomainEvents);
        });
        
        // ✅ InventoryExit (EDL Sortie)
        modelBuilder.Entity<InventoryExit>(entity =>
        {
            entity.ToTable("inventory_exits");
            entity.HasKey(ie => ie.Id);
            
            // TenantId (string) hérité de AuditableEntity pour multi-tenant
            entity.Property(ie => ie.TenantId).IsRequired().HasMaxLength(100);
            entity.HasIndex(ie => ie.TenantId);
            
            // RenterTenantId (Guid) pour le locataire du contrat
            entity.Property(ie => ie.RenterTenantId).IsRequired().HasColumnName("RenterTenantId");
            entity.HasIndex(ie => ie.RenterTenantId);
            
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
                comp.ToTable("inventory_comparisons");
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
            });
            
            // Degradations as owned entities
            entity.OwnsMany(ie => ie.Degradations, deg =>
            {
                deg.ToTable("inventory_degradations");
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
            });
            
            entity.Ignore(ie => ie.DomainEvents);
        });

        // Apply custom entity configurations
        modelBuilder.ApplyConfiguration(new Persistence.Configurations.PaymentConfiguration());
        modelBuilder.ApplyConfiguration(new Persistence.Configurations.RentInvoiceConfiguration());
    }
    
    /// <summary>
    /// Configure les Global Query Filters pour l'isolation multi-tenant
    /// Toutes les requêtes seront automatiquement filtrées par TenantId
    /// </summary>
    private void ConfigureMultiTenantFilters(ModelBuilder modelBuilder)
    {
        // Liste des types d'entités qui doivent être filtrés par tenant
        var tenantEntityTypes = new[]
        {
            typeof(Property),
            typeof(Tenant),
            typeof(Contract),
            typeof(Document),
            typeof(InventoryEntry),
            typeof(InventoryExit),
            typeof(RentabilityScenario),
            typeof(ScenarioVersion),
            typeof(ScenarioShare),
            typeof(UserSettings)
        };
        
        foreach (var entityType in tenantEntityTypes)
        {
            var method = typeof(LocaGuestDbContext)
                .GetMethod(nameof(ApplyTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            method?.MakeGenericMethod(entityType).Invoke(this, new object[] { modelBuilder });
        }
    }
    
    /// <summary>
    /// Applique un filtre global sur une entité pour filtrer par TenantId
    /// </summary>
    private void ApplyTenantFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : AuditableEntity
    {
        // IMPORTANT: Ne PAS capturer tenantId dans une variable locale
        // car cela fige la valeur au moment de OnModelCreating (avant l'authentification).
        // Le filtre doit évaluer _tenantContext à CHAQUE requête.
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => 
            _tenantContext == null || 
            !_tenantContext.IsAuthenticated || 
            _tenantContext.TenantId == null ||
            e.TenantId == _tenantContext.TenantId.Value.ToString());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Audit et isolation multi-tenant
        var entries = ChangeTracker.Entries<AuditableEntity>();
        foreach (var entry in entries)
        {
            var userId = _currentUserService?.UserId?.ToString() ?? "system";
            var now = DateTime.UtcNow;

            if (entry.State == EntityState.Added)
            {
                // Assignation automatique du TenantId
                if (string.IsNullOrEmpty(entry.Entity.TenantId))
                {
                    if (_tenantContext?.IsAuthenticated == true && _tenantContext.TenantId.HasValue)
                    {
                        entry.Entity.TenantId = _tenantContext.TenantId.Value.ToString();
                    }
                    else if (_tenantContext?.IsAuthenticated == true)
                    {
                        // User is authenticated but no TenantId in JWT
                        throw new UnauthorizedAccessException("Cannot create entity without a valid TenantId");
                    }
                    // else: Not authenticated (seeding, background jobs) - allow creation without TenantId
                }
                
                // Vérification que le TenantId correspond au tenant courant (only if authenticated)
                if (_tenantContext?.IsAuthenticated == true && 
                    !string.IsNullOrEmpty(entry.Entity.TenantId) &&
                    entry.Entity.TenantId != _tenantContext.TenantId.ToString())
                {
                    throw new UnauthorizedAccessException($"Cannot create entity for another tenant. Expected: {_tenantContext.TenantId}, Got: {entry.Entity.TenantId}");
                }
                
                entry.Entity.CreatedBy = userId;
                entry.Entity.CreatedAt = now;
            }

            if (entry.State == EntityState.Modified)
            {
                // Vérification que le TenantId n'a pas été modifié
                var originalTenantId = entry.Property(nameof(AuditableEntity.TenantId)).OriginalValue?.ToString();
                var currentTenantId = entry.Entity.TenantId;
                
                if (originalTenantId != currentTenantId)
                {
                    throw new InvalidOperationException("TenantId cannot be modified after entity creation");
                }
                
                // Vérification que l'entité appartient au tenant courant (only if authenticated)
                if (_tenantContext?.IsAuthenticated == true && 
                    !string.IsNullOrEmpty(entry.Entity.TenantId) &&
                    entry.Entity.TenantId != _tenantContext.TenantId.ToString())
                {
                    throw new UnauthorizedAccessException($"Cannot modify entity from another tenant");
                }
                
                entry.Entity.LastModifiedBy = userId;
                entry.Entity.LastModifiedAt = now;
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

