using LocaGuest.Domain.Aggregates.ContractAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocaGuest.Infrastructure.Persistence.Configurations;

public class ContractParticipantConfiguration : IEntityTypeConfiguration<ContractParticipant>
{
    public void Configure(EntityTypeBuilder<ContractParticipant> builder)
    {
        builder.ToTable("ContractParticipants");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.ContractId).IsRequired();
        builder.Property(p => p.TenantId).IsRequired();

        builder.Property(p => p.StartDate).IsRequired();
        builder.Property(p => p.EndDate);

        builder.Property(p => p.ShareType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.ShareValue)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.LastModifiedAt);

        builder.HasIndex(p => p.ContractId);
        builder.HasIndex(p => p.TenantId);
        builder.HasIndex(p => new { p.ContractId, p.TenantId, p.StartDate }).IsUnique();
        builder.HasIndex(p => p.StartDate);
        builder.HasIndex(p => p.EndDate);
    }
}
