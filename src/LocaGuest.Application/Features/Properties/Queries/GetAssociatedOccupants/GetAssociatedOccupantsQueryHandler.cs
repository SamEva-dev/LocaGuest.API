using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Occupants;
using LocaGuest.Domain.Aggregates.DocumentAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Properties.Queries.GetAssociatedOccupants;

public class GetAssociatedOccupantsQueryHandler : IRequestHandler<GetAssociatedOccupantsQuery, Result<List<OccupantDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetAssociatedOccupantsQueryHandler> _logger;

    public GetAssociatedOccupantsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetAssociatedOccupantsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<List<OccupantDto>>> Handle(GetAssociatedOccupantsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var propertyId = Guid.Parse(request.PropertyId);

            // Get occupants that are currently associated to this property
            var occupants = await _unitOfWork.Occupants.Query()
                .Where(t => t.PropertyId == propertyId)
                .OrderBy(t => t.FullName)
                .Select(t => new OccupantDto
                {
                    Id = t.Id,
                    Code = t.Code,
                    FullName = t.FullName,
                    Email = t.Email,
                    Phone = t.Phone,
                    Status = t.Status.ToString(),
                    ActiveContracts = 0,
                    MoveInDate = t.MoveInDate,
                    CreatedAt = t.CreatedAt,
                    PropertyId = t.PropertyId,
                    PropertyCode = t.PropertyCode,
                    HasIdentityDocument = false
                })
                .ToListAsync(cancellationToken);

            var occupantIds = occupants.Select(t => t.Id).Distinct().ToList();

            var identityDocOccupantIds = await _unitOfWork.Documents.Query()
                .Where(d => !d.IsArchived
                            && d.AssociatedOccupantId != null
                            && occupantIds.Contains(d.AssociatedOccupantId.Value)
                            && d.Type == DocumentType.PieceIdentite)
                .Select(d => d.AssociatedOccupantId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            var identityDocOccupantIdSet = identityDocOccupantIds.ToHashSet();
            foreach (var t in occupants)
            {
                t.HasIdentityDocument = identityDocOccupantIdSet.Contains(t.Id);
            }

            _logger.LogInformation("Found {Count} associated occupants for property {PropertyId}", 
                occupants.Count, propertyId);

            return Result.Success(occupants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving associated occupants for property {PropertyId}", request.PropertyId);
            return Result.Failure<List<OccupantDto>>("Error retrieving associated occupants");
        }
    }
}
