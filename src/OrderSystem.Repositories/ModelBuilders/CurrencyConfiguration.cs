using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderSystem.Models.Concrete;

namespace OrderSystem.Repositories.ModelBuilders;

public class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("Currencies");
        builder.HasKey(c => c.Id);

        // Fixed, seeded ids — not database-generated.
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Code).IsRequired().HasMaxLength(3);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(64);
        builder.HasIndex(c => c.Code).IsUnique();

        builder.HasData(
            new Currency(1, "USD", "US Dollar"),
            new Currency(2, "EUR", "Euro"),
            new Currency(3, "TRY", "Turkish Lira"),
            new Currency(4, "GBP", "British Pound"));
    }
}
