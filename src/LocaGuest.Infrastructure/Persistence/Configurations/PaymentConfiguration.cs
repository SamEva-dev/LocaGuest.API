using LocaGuest.Domain.Aggregates.PaymentAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocaGuest.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.TenantId)
            .IsRequired();

        builder.Property(p => p.PropertyId)
            .IsRequired();

        builder.Property(p => p.ContractId)
            .IsRequired();

        builder.Property(p => p.PaymentType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.AmountDue)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(p => p.AmountPaid)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(p => p.PaymentDate);

        builder.Property(p => p.ExpectedDate)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.Note)
            .HasMaxLength(1000);

        builder.Property(p => p.Month)
            .IsRequired();

        builder.Property(p => p.Year)
            .IsRequired();

        builder.Property(p => p.ReceiptId);

        builder.Property(p => p.InvoiceDocumentId);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.LastModifiedAt);

        // Indexes pour performance
        builder.HasIndex(p => p.TenantId);
        builder.HasIndex(p => p.PropertyId);
        builder.HasIndex(p => p.ContractId);
        builder.HasIndex(p => new { p.ContractId, p.Month, p.Year, p.PaymentType }).IsUnique();
        builder.HasIndex(p => p.Status);
    }
}
