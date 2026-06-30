using OrderSystem.Commands.HelpDesk.IngestDocument;
using OrderSystem.Common;
using OrderSystem.Services.Concrete;
using Xunit;

namespace OrderSystem.UnitTests.HelpDesk;

public class IngestDocumentCommandValidatorTests
{
    private static IngestDocumentCommandValidator Validator(long maxBytes = 1024) =>
        new(new DocumentTextExtractor(), new AppSettings { Rag = new RagSettings { MaxUploadBytes = maxBytes } });

    [Fact]
    public async Task Passes_for_supported_type_within_limit()
    {
        var command = new IngestDocumentCommand("Guide", "guide.md", "text/markdown", new byte[] { 1, 2, 3 });

        var result = await Validator().ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Fails_for_unsupported_type()
    {
        var command = new IngestDocumentCommand("Logo", "logo.png", "image/png", new byte[] { 1, 2, 3 });

        var result = await Validator().ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(IngestDocumentCommand.ContentType));
    }

    [Fact]
    public async Task Fails_when_over_size_limit()
    {
        var command = new IngestDocumentCommand("Big", "big.txt", "text/plain", new byte[2048]);

        var result = await Validator(maxBytes: 1024).ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(IngestDocumentCommand.Data));
    }

    [Fact]
    public async Task Fails_when_empty()
    {
        var command = new IngestDocumentCommand("Empty", "empty.txt", "text/plain", Array.Empty<byte>());

        var result = await Validator().ValidateAsync(command);

        Assert.False(result.IsValid);
    }
}
