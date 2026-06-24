using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderSystem.Models.Concrete;
using OrderSystem.Models.ValueObjects;

namespace OrderSystem.Repositories.ModelBuilders;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.CustomerId).IsRequired();
        builder.Ignore(o => o.CurrencyId); // derived from TotalAmount

        builder.Property(o => o.Status)
            .HasConversion(status => status.Value, value => OrderStatus.FromValue(value))
            .IsRequired()
            .HasMaxLength(32);
        builder.HasIndex(o => o.Status);

        builder.OwnsOne(o => o.TotalAmount, total =>
        {
            total.Property(m => m.Amount).HasColumnName("TotalAmount").HasColumnType("decimal(18,2)").IsRequired();
            total.Property(m => m.CurrencyId).HasColumnName("CurrencyId").IsRequired();
            total.HasOne<Currency>().WithMany().HasForeignKey(m => m.CurrencyId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Navigation(o => o.TotalAmount).IsRequired();

        // Items are a child collection exposed read-only and backed by the _items field.
        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        var itemsNav = builder.Metadata.FindNavigation(nameof(Order.Items))!;
        itemsNav.SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasQueryFilter(o => !o.IsDeleted);
        builder.Ignore(o => o.DomainEvents);
    }
}
