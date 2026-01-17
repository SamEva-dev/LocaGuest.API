using System.IO.Compression;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text;

namespace LocaGuest.Application.Features.Documents.Queries.ExportDocumentsZip;

public class ExportDocumentsZipQueryHandler : IRequestHandler<ExportDocumentsZipQuery, byte[]>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExportDocumentsZipQueryHandler> _logger;

    public ExportDocumentsZipQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<ExportDocumentsZipQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<byte[]> Handle(ExportDocumentsZipQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var OccupantId = Guid.Parse(request.OccupantId);
            var documents = await _unitOfWork.Documents.GetByTenantIdAsync(OccupantId, cancellationToken);
            var documentsList = documents.ToList();

            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var doc in documentsList)
                {
                    if (File.Exists(doc.FilePath))
                    {
                        // Create folder structure by category
                        var safeCategory = SanitizeZipEntrySegment(doc.Category.ToString());
                        var safeFileName = SanitizeZipEntrySegment(Path.GetFileName(doc.FileName));
                        var entryName = string.IsNullOrWhiteSpace(safeCategory)
                            ? safeFileName
                            : $"{safeCategory}/{safeFileName}";
                        var entry = archive.CreateEntry(entryName);

                        using var entryStream = entry.Open();
                        using var fileStream = File.OpenRead(doc.FilePath);
                        await fileStream.CopyToAsync(entryStream, cancellationToken);

                        _logger.LogDebug("Added {FileName} to ZIP", doc.FileName);
                    }
                    else
                    {
                        _logger.LogWarning("File not found: {FilePath}", doc.FilePath);
                    }
                }
            }

            _logger.LogInformation("Created ZIP with {Count} documents for tenant {OccupantId}", 
                documentsList.Count, request.OccupantId);

            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ZIP for tenant {OccupantId}", request.OccupantId);
            throw;
        }
    }

    private static string SanitizeZipEntrySegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            sb.Append(invalid.Contains(ch) ? '_' : ch);
        }

        var sanitized = sb.ToString();
        sanitized = sanitized.Replace("/", "_").Replace("\\", "_");
        return sanitized.Trim().TrimEnd('.');
    }
}
