using LocaGuest.Domain.Aggregates.PaymentAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocaGuest.Infrastructure.Persistence.Configurations;

public class RentInvoiceLineConfiguration : IEntityTypeConfiguration<RentInvoiceLine>
{
    public void Configure(EntityTypeBuilder<RentInvoiceLine> builder)
    {
        builder.ToTable("RentInvoiceLines");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.RentInvoiceId).IsRequired();
        builder.Property(l => l.TenantId).IsRequired();

        builder.Property(l => l.AmountDue)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(l => l.AmountPaid)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(l => l.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(l => l.ShareType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(l => l.ShareValue)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(l => l.PaymentId);
        builder.Property(l => l.PaidDate);

        builder.Property(l => l.CreatedAt).IsRequired();
        builder.Property(l => l.LastModifiedAt);

        builder.HasIndex(l => l.RentInvoiceId);
        builder.HasIndex(l => l.TenantId);
        builder.HasIndex(l => new { l.RentInvoiceId, l.TenantId }).IsUnique();
        builder.HasIndex(l => l.Status);
    }
}
