using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OrderSystem.Services.Abstract;
using UglyToad.PdfPig;

namespace OrderSystem.Services.Concrete;

/// <summary>
/// Extracts plain text from PDF (PdfPig), Word .docx (OpenXml) and plain
/// text/markdown uploads, dispatching by file extension with a content-type
/// fallback. All formats are pure-managed — no native dependencies.
/// </summary>
public class DocumentTextExtractor : IDocumentTextExtractor
{
    public bool CanExtract(string contentType, string fileName) =>
        ResolveFormat(contentType, fileName) is not DocumentFormat.Unsupported;

    public string Extract(string contentType, string fileName, byte[] data) =>
        ResolveFormat(contentType, fileName) switch
        {
            DocumentFormat.Pdf => ExtractPdf(data),
            DocumentFormat.Word => ExtractWord(data),
            DocumentFormat.PlainText => ExtractPlainText(data),
            _ => throw new NotSupportedException($"Unsupported document type '{contentType}' ({fileName})."),
        };

    private static DocumentFormat ResolveFormat(string contentType, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        switch (extension)
        {
            case ".pdf": return DocumentFormat.Pdf;
            case ".docx": return DocumentFormat.Word;
            case ".txt" or ".md" or ".markdown" or ".text": return DocumentFormat.PlainText;
        }

        var type = (contentType ?? string.Empty).ToLowerInvariant();
        if (type.Contains("pdf"))
            return DocumentFormat.Pdf;
        if (type.Contains("wordprocessingml") || type.Contains("msword"))
            return DocumentFormat.Word;
        if (type.StartsWith("text/"))
            return DocumentFormat.PlainText;

        return DocumentFormat.Unsupported;
    }

    private static string ExtractPdf(byte[] data)
    {
        using var document = PdfDocument.Open(data);
        var builder = new StringBuilder();
        foreach (var page in document.GetPages())
            builder.AppendLine(page.Text);
        return builder.ToString();
    }

    private static string ExtractWord(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var document = WordprocessingDocument.Open(stream, false);
        var body = document.MainDocumentPart?.Document?.Body;
        if (body is null)
            return string.Empty;

        var builder = new StringBuilder();
        foreach (var paragraph in body.Descendants<Paragraph>())
            builder.AppendLine(paragraph.InnerText);
        return builder.ToString();
    }

    private static string ExtractPlainText(byte[] data) => Encoding.UTF8.GetString(data);

    private enum DocumentFormat
    {
        Unsupported,
        Pdf,
        Word,
        PlainText,
    }
}
