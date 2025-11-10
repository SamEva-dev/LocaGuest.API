using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Aggregates.TenantAggregate;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Aggregates.UserAggregate;
using LocaGuest.Domain.Common;
using LocaGuest.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LocaGuest.Application.Services;

namespace LocaGuest.Infrastructure.Persistence;

public class LocaGuestDbContext : DbContext, ILocaGuestDbContext
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService? _currentUserService;

    public LocaGuestDbContext(
        DbContextOptions<LocaGuestDbContext> options,
        IMediator mediator,
        ICurrentUserService? currentUserService = null)
        : base(options)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Property
        modelBuilder.Entity<Property>(entity =>
        {
            entity.ToTable("properties");
            entity.HasKey(p => p.Id);
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
        });

        // Tenant
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("tenants");
            entity.HasKey(t => t.Id);
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
            entity.Property(c => c.Rent).HasColumnType("decimal(18,2)");
            entity.Property(c => c.Deposit).HasColumnType("decimal(18,2)");
            
            entity.HasMany(c => c.Payments)
                  .WithOne()
                  .HasForeignKey(p => p.ContractId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Ignore(c => c.DomainEvents);
        });

        // Payment
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Amount).HasColumnType("decimal(18,2)");
            entity.Ignore(p => p.DomainEvents);
        });

        // UserSettings
        modelBuilder.Entity<UserSettings>(entity =>
        {
            entity.ToTable("user_settings");
            entity.HasKey(us => us.Id);
            entity.HasIndex(us => us.UserId).IsUnique();
            entity.Property(us => us.Language).IsRequired().HasMaxLength(10);
            entity.Property(us => us.Timezone).IsRequired().HasMaxLength(100);
            entity.Property(us => us.DateFormat).IsRequired().HasMaxLength(20);
            entity.Property(us => us.Currency).IsRequired().HasMaxLength(10);
            entity.Property(us => us.PhotoUrl).HasMaxLength(500);
            entity.Ignore(us => us.DomainEvents);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Audit
        var entries = ChangeTracker.Entries<AuditableEntity>();
        foreach (var entry in entries)
        {
            var userId = _currentUserService?.UserId ?? "system";
            var now = DateTime.UtcNow;

            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedBy = userId;
                entry.Entity.CreatedAt = now;
            }

            if (entry.State == EntityState.Modified || entry.State == EntityState.Added)
            {
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

