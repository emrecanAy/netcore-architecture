using OrderSystem.Models.Concrete;
using OrderSystem.Models.ValueObjects;
using Xunit;

namespace OrderSystem.UnitTests.HelpDesk;

public class KnowledgeDocumentTests
{
    private static KnowledgeDocument NewDocument() =>
        KnowledgeDocument.Create("Manual", "manual.pdf", "application/pdf", new byte[] { 1, 2, 3 });

    [Fact]
    public void Create_starts_pending_with_stored_bytes()
    {
        var document = NewDocument();

        Assert.Equal(DocumentStatus.Pending, document.Status);
        Assert.Equal(0, document.ChunkCount);
        Assert.NotNull(document.RawData);
    }

    [Fact]
    public void Create_rejects_empty_upload()
    {
        Assert.Throws<ArgumentException>(() =>
            KnowledgeDocument.Create("Manual", "manual.pdf", "application/pdf", Array.Empty<byte>()));
    }

    [Fact]
    public void MarkIndexed_sets_count_and_releases_bytes()
    {
        var document = NewDocument();

        document.MarkProcessing();
        document.MarkIndexed(7);

        Assert.Equal(DocumentStatus.Indexed, document.Status);
        Assert.Equal(7, document.ChunkCount);
        Assert.Null(document.RawData);
        Assert.Null(document.ErrorMessage);
    }

    [Fact]
    public void MarkFailed_records_error_and_keeps_bytes_for_retry()
    {
        var document = NewDocument();

        document.MarkProcessing();
        document.MarkFailed("boom");

        Assert.Equal(DocumentStatus.Failed, document.Status);
        Assert.Equal("boom", document.ErrorMessage);
        Assert.NotNull(document.RawData);
    }

    [Fact]
    public void DocumentStatus_FromValue_rejects_unknown()
    {
        Assert.Throws<ArgumentException>(() => DocumentStatus.FromValue("Nope"));
    }
}
