using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderSystem.Models.Concrete;

namespace OrderSystem.Repositories.ModelBuilders;

public class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ToTable("DocumentChunks");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.DocumentId).IsRequired();
        builder.Property(c => c.Ordinal).IsRequired();
        builder.Property(c => c.Content).IsRequired();
        builder.Property(c => c.TokenEstimate).IsRequired();

        // Retrieval loads chunks by id; deletion removes them by document.
        builder.HasIndex(c => c.DocumentId);

        builder.HasQueryFilter(c => !c.IsDeleted);
        builder.Ignore(c => c.DomainEvents);
    }
}
