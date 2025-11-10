using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Documents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Documents.Queries.GetDocumentStats;

public class GetDocumentStatsQueryHandler : IRequestHandler<GetDocumentStatsQuery, Result<DocumentStatsDto>>
{
    private readonly ILogger<GetDocumentStatsQueryHandler> _logger;

    public GetDocumentStatsQueryHandler(ILogger<GetDocumentStatsQueryHandler> logger)
    {
        _logger = logger;
    }

    public async Task<Result<DocumentStatsDto>> Handle(GetDocumentStatsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Simulation de stats (à remplacer par vraies données de DB)
            var stats = new DocumentStatsDto
            {
                TotalDocuments = 2847,
                ThisMonthDocuments = 156,
                ActiveTemplates = 8,
                TimeSavedHours = 127
            };

            return await Task.FromResult(Result.Success(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document stats");
            return Result.Failure<DocumentStatsDto>("Error retrieving document stats");
        }
    }
}
