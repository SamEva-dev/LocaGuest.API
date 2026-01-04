using LocaGuest.Domain.Aggregates.ContractAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocaGuest.Infrastructure.Persistence.Configurations;

public class AddendumConfiguration : IEntityTypeConfiguration<Addendum>
{
    public void Configure(EntityTypeBuilder<Addendum> builder)
    {
        builder.ToTable("addendums", schema: "lease");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.ContractId)
            .IsRequired();

        builder.Property(a => a.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.EffectiveDate)
            .IsRequired();

        builder.Property(a => a.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.Description)
            .IsRequired()
            .HasMaxLength(2000);

        // Financial changes
        builder.Property(a => a.OldRent)
            .HasPrecision(10, 2);

        builder.Property(a => a.NewRent)
            .HasPrecision(10, 2);

        builder.Property(a => a.OldCharges)
            .HasPrecision(10, 2);

        builder.Property(a => a.NewCharges)
            .HasPrecision(10, 2);

        // Duration changes
        builder.Property(a => a.OldEndDate);

        builder.Property(a => a.NewEndDate);

        // Occupants changes (JSON)
        builder.Property(a => a.OccupantChanges)
            .HasMaxLength(4000);

        // Room changes
        builder.Property(a => a.OldRoomId);

        builder.Property(a => a.NewRoomId);

        // Clauses changes
        builder.Property(a => a.OldClauses)
            .HasMaxLength(2000);

        builder.Property(a => a.NewClauses)
            .HasMaxLength(2000);

        // Documents (JSON array of IDs)
        builder.Property(a => a.AttachedDocumentIds)
            .HasMaxLength(1000);

        // Signature
        builder.Property(a => a.SignatureStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.SignedDate);

        // Notes
        builder.Property(a => a.Notes)
            .HasMaxLength(1000);

        // Indexes
        builder.HasIndex(a => a.ContractId);
        builder.HasIndex(a => a.Type);
        builder.HasIndex(a => a.EffectiveDate);
        builder.HasIndex(a => a.SignatureStatus);
        builder.HasIndex(a => a.CreatedAt);
    }
}
