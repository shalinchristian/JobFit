using System.Text;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;

namespace JobFit.Services;

/// <summary>
/// Extracts plain text from uploaded resume or job description files
/// (.pdf, .docx, .txt, .md).
/// </summary>
public static class DocumentTextExtractor
{
    /// <summary>The only types the UI offers and the server accepts (one list, used by both).</summary>
    public static readonly string[] AllowedExtensions = { ".pdf", ".docx", ".txt", ".md" };
    public const string HumanTypes = "PDF, DOCX, TXT or MD";
    public static string AcceptAttribute => string.Join(",", AllowedExtensions);

    /// <summary>Per-file limit (checked in the browser and on the server).</summary>
    public const long MaxFileBytes = 10L * 1024 * 1024;
    /// <summary>Whole-request limit: two max-size files plus form fields, so a too-big file still reaches validation.</summary>
    public const long MaxRequestBytes = 64L * 1024 * 1024;

    public static string EmptyMessage(string fileName) =>
        $"\"{fileName}\" is empty. Choose a file that has text in it (scanned image-only PDFs have none).";

    /// <summary>Max characters accepted for pasted/JSON text (resume or posting) on the API.</summary>
    public const int MaxTextChars = 1_000_000;

    public static string TooLargeMessage(string? fileName) =>
        $"{(string.IsNullOrEmpty(fileName) ? "That file" : $"\"{fileName}\"")} is too large. Files can be up to {MaxFileBytes / (1024 * 1024)} MB; a resume is usually under 1 MB.";

    /// <summary>
    /// Reads text from uploaded files. This demo supports plain text formats only.
    /// </summary>
    public static async Task<string> ExtractPlainTextAsync(Stream source, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            throw new ArgumentException($"Unsupported file type. Upload a {HumanTypes} file.");

        if (source.CanSeek && source.Length > MaxFileBytes)
            throw new ArgumentException(TooLargeMessage(fileName));

        string content = extension switch
        {
            ".pdf" => await ExtractPdfTextAsync(source, fileName),
            ".docx" => await ExtractDocxTextAsync(source, fileName),
            _ => await ExtractTextFileAsync(source, fileName)
        };

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException(EmptyMessage(fileName));

        return content.Trim();
    }

    private static async Task<string> ExtractTextFileAsync(Stream source, string fileName)
    {
        using var reader = new StreamReader(source, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return await reader.ReadToEndAsync();
    }

    private static async Task<string> ExtractPdfTextAsync(Stream source, string fileName)
    {
        using var memory = new MemoryStream();
        await source.CopyToAsync(memory);
        memory.Position = 0;

        var builder = new StringBuilder();
        using var pdf = PdfDocument.Open(memory);
        foreach (var page in pdf.GetPages())
        {
            builder.AppendLine(page.Text);
        }

        return builder.ToString();
    }

    private static async Task<string> ExtractDocxTextAsync(Stream source, string fileName)
    {
        using var memory = new MemoryStream();
        await source.CopyToAsync(memory);
        memory.Position = 0;

        using var document = WordprocessingDocument.Open(memory, false);
        var bodyText = document.MainDocumentPart?.Document.Body?.InnerText;
        return bodyText ?? string.Empty;
    }
}
