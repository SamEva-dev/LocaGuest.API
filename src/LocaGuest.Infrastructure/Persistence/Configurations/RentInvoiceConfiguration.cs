using LocaGuest.Domain.Aggregates.PaymentAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocaGuest.Infrastructure.Persistence.Configurations;

public class RentInvoiceConfiguration : IEntityTypeConfiguration<RentInvoice>
{
    public void Configure(EntityTypeBuilder<RentInvoice> builder)
    {
        builder.ToTable("rent_invoices", schema: "finance");

        builder.HasKey(ri => ri.Id);

        builder.Property(ri => ri.ContractId)
            .IsRequired();

        builder.Property(ri => ri.OrganizationId)
            .IsRequired();

        builder.Property(ri => ri.PropertyId)
            .IsRequired();

        builder.Property(ri => ri.Month)
            .IsRequired();

        builder.Property(ri => ri.Year)
            .IsRequired();

        builder.Property(ri => ri.Amount)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(ri => ri.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(ri => ri.PaymentId);

        builder.Property(ri => ri.InvoiceDocumentId);

        builder.Property(ri => ri.GeneratedAt)
            .IsRequired();

        builder.Property(ri => ri.DueDate)
            .IsRequired();

        builder.Property(ri => ri.CreatedAt)
            .IsRequired();

        builder.Property(ri => ri.LastModifiedAt);

        // Indexes pour performance
        builder.HasIndex(ri => ri.ContractId);
        builder.HasIndex(ri => ri.OrganizationId);
        builder.HasIndex(ri => ri.PropertyId);
        builder.HasIndex(ri => new { ri.ContractId, ri.Month, ri.Year }).IsUnique();
        builder.HasIndex(ri => ri.Status);
        builder.HasIndex(ri => ri.DueDate);
    }
}
