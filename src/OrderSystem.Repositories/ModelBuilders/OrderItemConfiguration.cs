using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderSystem.Models.Concrete;

namespace OrderSystem.Repositories.ModelBuilders;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.ProductId).IsRequired();
        builder.Property(i => i.ProductName).IsRequired().HasMaxLength(200);
        builder.Property(i => i.Quantity).IsRequired();

        builder.OwnsOne(i => i.UnitPrice, price =>
        {
            price.Property(m => m.Amount).HasColumnName("UnitPrice").HasColumnType("decimal(18,2)").IsRequired();
            price.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });
        builder.Navigation(i => i.UnitPrice).IsRequired();

        builder.Ignore(i => i.LineTotal);
        builder.Ignore(i => i.DomainEvents);
    }
}
