using LocaGuest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocaGuest.Infrastructure.Persistence.Configurations;

public sealed class EmailDeliveryEventConfiguration : IEntityTypeConfiguration<EmailDeliveryEvent>
{
    public void Configure(EntityTypeBuilder<EmailDeliveryEvent> builder)
    {
        builder.ToTable("email_delivery_events", schema: "messaging");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MessageId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Email)
            .HasMaxLength(320);

        builder.Property(x => x.EventType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.TsEvent)
            .IsRequired();

        builder.Property(x => x.RawPayload)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.CreatedAtUtc);

        builder.HasIndex(x => new { x.MessageId, x.EventType, x.TsEvent })
            .IsUnique();
    }
}
