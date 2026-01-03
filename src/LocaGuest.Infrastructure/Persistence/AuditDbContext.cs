using Microsoft.EntityFrameworkCore;
using LocaGuest.Domain.Audit;
using LocaGuest.Application.Common.Interfaces;

namespace LocaGuest.Infrastructure.Persistence;

/// <summary>
/// Dedicated database context for audit logs (separate from main business database)
/// </summary>
public class AuditDbContext : DbContext, IAuditDbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options)
    {
    }
    
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<CommandAuditLog> CommandAuditLogs => Set<CommandAuditLog>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // AuditLog configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(200);
            entity.Property(e => e.EntityId).HasMaxLength(100);
            entity.Property(e => e.UserEmail).HasMaxLength(256);
            entity.Property(e => e.IpAddress).IsRequired().HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.RequestPath).HasMaxLength(500);
            entity.Property(e => e.HttpMethod).HasMaxLength(10);
            entity.Property(e => e.CorrelationId).HasMaxLength(100);
            entity.Property(e => e.SessionId).HasMaxLength(100);

            entity.Property(e => e.OrganizationId).HasColumnName("TenantId");
            
            // JSON columns for flexibility
            entity.Property(e => e.OldValues).HasColumnType("jsonb");
            entity.Property(e => e.NewValues).HasColumnType("jsonb");
            entity.Property(e => e.Changes).HasColumnType("jsonb");
            entity.Property(e => e.AdditionalData).HasColumnType("jsonb");
            
            // Indexes for performance
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.EntityType);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.HasIndex(e => e.CorrelationId);
        });
        
        // CommandAuditLog configuration
        modelBuilder.Entity<CommandAuditLog>(entity =>
        {
            entity.ToTable("CommandAuditLogs");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.CommandName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UserEmail).HasMaxLength(256);
            entity.Property(e => e.IpAddress).IsRequired().HasMaxLength(45);
            entity.Property(e => e.CorrelationId).HasMaxLength(100);
            entity.Property(e => e.RequestPath).HasMaxLength(500);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);

            entity.Property(e => e.OrganizationId).HasColumnName("TenantId");
            
            // JSON columns
            entity.Property(e => e.CommandData).HasColumnType("jsonb");
            entity.Property(e => e.ResultData).HasColumnType("jsonb");
            entity.Property(e => e.StackTrace).HasColumnType("text");
            
            // Indexes
            entity.HasIndex(e => e.ExecutedAt);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.CommandName);
            entity.HasIndex(e => e.Success);
            entity.HasIndex(e => e.CorrelationId);
        });
    }
}
