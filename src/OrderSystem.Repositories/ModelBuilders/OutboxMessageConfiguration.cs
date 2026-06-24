using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderSystem.Repositories.Outbox;

namespace OrderSystem.Repositories.ModelBuilders;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Type).IsRequired().HasMaxLength(512);
        builder.Property(m => m.Content).IsRequired();
        builder.Property(m => m.OccurredOn).IsRequired();
        builder.Property(m => m.Error).HasMaxLength(2048);

        // Dispatcher polls unprocessed messages oldest-first.
        builder.HasIndex(m => m.ProcessedOn);
    }
}
