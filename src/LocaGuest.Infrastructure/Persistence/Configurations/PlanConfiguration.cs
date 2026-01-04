using LocaGuest.Domain.Aggregates.SubscriptionAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocaGuest.Infrastructure.Persistence.Configurations;

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("plans", schema: "billing");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Code)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(p => p.Description)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.Property(p => p.MonthlyPrice)
            .HasColumnType("decimal(18,2)");
        
        builder.Property(p => p.AnnualPrice)
            .HasColumnType("decimal(18,2)");
        
        builder.Property(p => p.StripeMonthlyPriceId)
            .HasMaxLength(255);
        
        builder.Property(p => p.StripeAnnualPriceId)
            .HasMaxLength(255);
        
        builder.HasIndex(p => p.Code)
            .IsUnique();
        
        builder.HasIndex(p => p.SortOrder);
    }
}
