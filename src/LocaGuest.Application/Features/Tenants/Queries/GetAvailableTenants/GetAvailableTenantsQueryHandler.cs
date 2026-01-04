using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Tenants;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Tenants.Queries.GetAvailableTenants;

public class GetAvailableTenantsQueryHandler : IRequestHandler<GetAvailableTenantsQuery, Result<List<TenantDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetAvailableTenantsQueryHandler> _logger;

    public GetAvailableTenantsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetAvailableTenantsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<List<TenantDto>>> Handle(GetAvailableTenantsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var propertyId = Guid.Parse(request.PropertyId);

            // ✅ Récupérer UNIQUEMENT les locataires qui ne sont associés à AUCUN bien
            // Un locataire ne peut être associé qu'à un seul bien à la fois
            var availableTenants = await _unitOfWork.Occupants.Query()
                .Where(t => t.PropertyId == null)
                .Select(t => new TenantDto
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
                "Found {Count} available tenants for property {PropertyId}", 
                availableTenants.Count, 
                propertyId);

            return Result.Success(availableTenants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available tenants for property {PropertyId}", request.PropertyId);
            return Result.Failure<List<TenantDto>>("Error retrieving available tenants");
        }
    }
}
