using LocaGuest.Domain.Aggregates.AnalyticsAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocaGuest.Infrastructure.Persistence.Configurations;

public class SatisfactionFeedbackConfiguration : IEntityTypeConfiguration<SatisfactionFeedback>
{
    public void Configure(EntityTypeBuilder<SatisfactionFeedback> builder)
    {
        builder.ToTable("satisfaction_feedback", schema: "analytics");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrganizationId);
        builder.Property(x => x.UserId);

        builder.Property(x => x.Rating)
            .IsRequired();

        builder.Property(x => x.Comment)
            .HasMaxLength(2000);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.CreatedAtUtc);
        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => x.UserId);
    }
}
