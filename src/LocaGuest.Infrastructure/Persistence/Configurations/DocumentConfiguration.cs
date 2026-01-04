using LocaGuest.Domain.Aggregates.DocumentAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocaGuest.Infrastructure.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents", schema: "doc");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.FileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(d => d.FilePath)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(d => d.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(d => d.Category)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(d => d.FileSizeBytes)
            .IsRequired();

        builder.Property(d => d.Description)
            .HasMaxLength(1000);

        builder.Property(d => d.ExpiryDate);

        builder.Property(d => d.AssociatedTenantId);

        builder.Property(d => d.PropertyId);

        builder.Property(d => d.IsArchived)
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(d => d.Code).IsUnique();
        builder.HasIndex(d => d.AssociatedTenantId);
        builder.HasIndex(d => d.PropertyId);
        builder.HasIndex(d => d.Type);
        builder.HasIndex(d => d.Category);
        builder.HasIndex(d => d.IsArchived);
    }
}
