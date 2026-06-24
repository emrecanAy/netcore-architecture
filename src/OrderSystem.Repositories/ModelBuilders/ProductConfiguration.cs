using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderSystem.Models.Concrete;
using OrderSystem.Models.ValueObjects;

namespace OrderSystem.Repositories.ModelBuilders;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);

        builder.Property(p => p.Sku)
            .HasConversion(sku => sku.Value, value => new Sku(value))
            .HasColumnName("Sku")
            .IsRequired()
            .HasMaxLength(64);
        builder.HasIndex(p => p.Sku).IsUnique();

        // Money is a composite VO mapped to two columns via an owned type.
        builder.OwnsOne(p => p.Price, price =>
        {
            price.Property(m => m.Amount).HasColumnName("Price").HasColumnType("decimal(18,2)").IsRequired();
            price.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });
        builder.Navigation(p => p.Price).IsRequired();

        builder.Property(p => p.StockQuantity).IsRequired();
        builder.Property(p => p.IsActive).IsRequired();

        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.Ignore(p => p.DomainEvents);
    }
}
