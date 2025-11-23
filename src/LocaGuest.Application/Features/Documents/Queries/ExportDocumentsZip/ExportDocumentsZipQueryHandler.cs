using System.IO.Compression;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

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
            var tenantId = Guid.Parse(request.TenantId);
            var documents = await _unitOfWork.Documents.GetByTenantIdAsync(tenantId, cancellationToken);

            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var doc in documents)
                {
                    if (File.Exists(doc.FilePath))
                    {
                        // Create folder structure by category
                        var entryName = $"{doc.Category}/{doc.FileName}";
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

            _logger.LogInformation("Created ZIP with {Count} documents for tenant {TenantId}", 
                documents.Count, tenantId);

            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ZIP for tenant {TenantId}", request.TenantId);
            throw;
        }
    }
}
