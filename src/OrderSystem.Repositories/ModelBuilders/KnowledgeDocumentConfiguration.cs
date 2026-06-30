using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderSystem.Models.Concrete;
using OrderSystem.Models.ValueObjects;

namespace OrderSystem.Repositories.ModelBuilders;

public class KnowledgeDocumentConfiguration : IEntityTypeConfiguration<KnowledgeDocument>
{
    public void Configure(EntityTypeBuilder<KnowledgeDocument> builder)
    {
        builder.ToTable("KnowledgeDocuments");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Title).IsRequired().HasMaxLength(256);
        builder.Property(d => d.FileName).IsRequired().HasMaxLength(256);
        builder.Property(d => d.ContentType).IsRequired().HasMaxLength(128);

        // Status is an enum-like value object stored as its string code.
        builder.Property(d => d.Status)
            .HasConversion(s => s.Value, value => DocumentStatus.FromValue(value))
            .HasColumnName("Status")
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(d => d.ChunkCount).IsRequired();
        builder.Property(d => d.ErrorMessage).HasMaxLength(2048);
        builder.Property(d => d.RawData);

        // The ingestion worker polls pending documents by status.
        builder.HasIndex(d => d.Status);

        builder.HasQueryFilter(d => !d.IsDeleted);
        builder.Ignore(d => d.DomainEvents);
    }
}
