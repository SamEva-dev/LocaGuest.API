using LocaGuest.Domain.Aggregates.DepositAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocaGuest.Infrastructure.Persistence.Configurations;

public class DepositConfiguration : IEntityTypeConfiguration<Deposit>
{
    public void Configure(EntityTypeBuilder<Deposit> builder)
    {
        builder.ToTable("deposits", schema: "finance");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .ValueGeneratedNever();

        builder.Property(d => d.OrganizationId).IsRequired();
        builder.Property(d => d.ContractId).IsRequired();

        builder.Property(d => d.AmountExpected)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(d => d.DueDate)
            .IsRequired();

        builder.Property(d => d.AllowInstallments)
            .IsRequired();

        builder.Property(d => d.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(d => d.CreatedAt)
            .IsRequired();

        builder.Property(d => d.LastModifiedAt);

        builder.HasMany(d => d.Transactions)
            .WithOne()
            .HasForeignKey(t => t.DepositId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(d => d.Transactions)
            .HasField("_transactions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(d => d.OrganizationId);
        builder.HasIndex(d => d.ContractId);
        builder.HasIndex(d => new { d.OrganizationId, d.ContractId }).IsUnique();
    }
}

public class DepositTransactionConfiguration : IEntityTypeConfiguration<DepositTransaction>
{
    public void Configure(EntityTypeBuilder<DepositTransaction> builder)
    {
        builder.ToTable("deposit_transactions", schema: "finance");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.DepositId).IsRequired();

        builder.Property(t => t.Kind)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Amount)
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(t => t.DateUtc).IsRequired();

        builder.Property(t => t.Reference)
            .HasMaxLength(500);

        builder.HasIndex(t => t.DepositId);
        builder.HasIndex(t => t.DateUtc);
    }
}
