using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Occupants;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Occupants.Queries.GetAvailableOccupants;

public class GetAvailableOccupantsQueryHandler : IRequestHandler<GetAvailableOccupantsQuery, Result<List<OccupantDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrganizationContext _orgContext;
    private readonly ILogger<GetAvailableOccupantsQueryHandler> _logger;

    public GetAvailableOccupantsQueryHandler(
        IUnitOfWork unitOfWork,
        IOrganizationContext orgContext,
        ILogger<GetAvailableOccupantsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _orgContext = orgContext;
        _logger = logger;
    }

    public async Task<Result<List<OccupantDto>>> Handle(GetAvailableOccupantsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_orgContext.IsAuthenticated || !_orgContext.OrganizationId.HasValue)
            {
                return Result.Failure<List<OccupantDto>>("User not authenticated");
            }

            if (!Guid.TryParse(request.PropertyId, out var propertyId))
            {
                return Result.Failure<List<OccupantDto>>($"Invalid property ID format: {request.PropertyId}");
            }

            var propertyExists = await _unitOfWork.Properties.Query()
                .AnyAsync(
                    p => p.Id == propertyId && p.OrganizationId == _orgContext.OrganizationId.Value,
                    cancellationToken);

            if (!propertyExists)
            {
                return Result.Failure<List<OccupantDto>>("Property not found");
            }

            // ✅ Récupérer UNIQUEMENT les occupants qui ne sont associés à AUCUN bien
            var availableOccupants = await _unitOfWork.Occupants.Query()
                .Where(t => t.OrganizationId == _orgContext.OrganizationId.Value && t.PropertyId == null)
                .Select(t => new OccupantDto
                {
                    Id = t.Id,
                    Code = t.Code,
                    FullName = t.FullName,
                    Email = t.Email,
                    Phone = t.Phone,
                    Status = t.Status.ToString(),
                    MoveInDate = t.MoveInDate,
                    CreatedAt = t.CreatedAt,
                    HasIdentityDocument = false,
                    PropertyId = t.PropertyId,
                    PropertyCode = t.PropertyCode
                })
                .OrderBy(t => t.FullName)
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Found {Count} available occupants for property {PropertyId}", 
                availableOccupants.Count, 
                propertyId);

            return Result.Success(availableOccupants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available occupants for property {PropertyId}", request.PropertyId);
            return Result.Failure<List<OccupantDto>>("Error retrieving available occupants");
        }
    }
}
