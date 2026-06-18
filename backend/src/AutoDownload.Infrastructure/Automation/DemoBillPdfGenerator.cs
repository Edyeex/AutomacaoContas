using System.Globalization;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AutoDownload.Infrastructure.Automation;

internal sealed record DemoBillDocument(
    Guid UserId,
    Guid AccountId,
    string OperatorName,
    string Reference,
    DateOnly DueDate,
    decimal Amount,
    string FileName);

internal interface IDemoBillPdfGenerator
{
    Task<string> GenerateAsync(DemoBillDocument document, CancellationToken cancellationToken = default);
}

internal sealed class DemoBillPdfGenerator : IDemoBillPdfGenerator
{
    private readonly string storageRoot;

    public DemoBillPdfGenerator(
        IOptions<DemoAutomationOptions> options,
        IHostEnvironment hostEnvironment)
    {
        var configuredDirectory = options.Value.StorageDirectory;
        if (string.IsNullOrWhiteSpace(configuredDirectory))
        {
            throw new InvalidOperationException("Automation:Demo:StorageDirectory is required.");
        }

        storageRoot = Path.GetFullPath(
            Path.IsPathRooted(configuredDirectory)
                ? configuredDirectory
                : Path.Combine(hostEnvironment.ContentRootPath, configuredDirectory));
    }

    public async Task<string> GenerateAsync(
        DemoBillDocument document,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var safeFileName = Path.GetFileName(document.FileName);
        if (!string.Equals(safeFileName, document.FileName, StringComparison.Ordinal) ||
            !safeFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Demo bill file name is invalid.");
        }

        var directory = Path.Combine(storageRoot, document.UserId.ToString("N"), document.AccountId.ToString("N"));
        Directory.CreateDirectory(directory);

        var destinationPath = Path.Combine(directory, safeFileName);
        var temporaryPath = $"{destinationPath}.{Guid.NewGuid():N}.tmp";
        var pdf = BuildPdf(document);

        try
        {
            await File.WriteAllBytesAsync(temporaryPath, pdf, cancellationToken);
            File.Move(temporaryPath, destinationPath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }

        return destinationPath;
    }

    private static byte[] BuildPdf(DemoBillDocument document)
    {
        var lines = new[]
        {
            "AutoDownload - Boleto de demonstracao",
            $"Operadora: {document.OperatorName}",
            $"Referencia: {document.Reference}",
            $"Vencimento: {document.DueDate:dd/MM/yyyy}",
            $"Valor: R$ {document.Amount.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"))}",
            "Documento gerado em ambiente controlado para demonstracao."
        };

        var content = BuildPageContent(lines);
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };

        using var stream = new MemoryStream();
        WriteAscii(stream, "%PDF-1.4\n% AutoDownload demo document\n");

        var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Length; index++)
        {
            offsets.Add(stream.Position);
            WriteAscii(stream, $"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }

        var crossReferenceOffset = stream.Position;
        WriteAscii(stream, $"xref\n0 {objects.Length + 1}\n");
        WriteAscii(stream, "0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            WriteAscii(stream, $"{offset:0000000000} 00000 n \n");
        }

        WriteAscii(
            stream,
            $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{crossReferenceOffset}\n%%EOF\n");

        return stream.ToArray();
    }

    private static string BuildPageContent(IEnumerable<string> lines)
    {
        var builder = new StringBuilder();
        builder.AppendLine("BT");
        builder.AppendLine("/F1 18 Tf");
        builder.AppendLine("50 780 Td");

        var firstLine = true;
        foreach (var line in lines)
        {
            if (!firstLine)
            {
                builder.AppendLine("0 -32 Td");
                builder.AppendLine("/F1 12 Tf");
            }

            builder.Append('(').Append(EscapePdfText(line)).AppendLine(") Tj");
            firstLine = false;
        }

        builder.Append("ET");
        return builder.ToString();
    }

    private static string EscapePdfText(string value)
    {
        var asciiValue = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(value));
        return asciiValue
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
    }

    private static void WriteAscii(Stream stream, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }
}
