using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Tenants;
using LocaGuest.Domain.Aggregates.DocumentAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Properties.Queries.GetAssociatedTenants;

public class GetAssociatedTenantsQueryHandler : IRequestHandler<GetAssociatedTenantsQuery, Result<List<TenantDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetAssociatedTenantsQueryHandler> _logger;

    public GetAssociatedTenantsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetAssociatedTenantsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<List<TenantDto>>> Handle(GetAssociatedTenantsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var propertyId = Guid.Parse(request.PropertyId);

            // Get tenants that are currently associated to this property
            var tenants = await _unitOfWork.Occupants.Query()
                .Where(t => t.PropertyId == propertyId)
                .OrderBy(t => t.FullName)
                .Select(t => new TenantDto
                {
                    Id = t.Id,
                    Code = t.Code,
                    FullName = t.FullName,
                    Email = t.Email,
                    Phone = t.Phone,
                    Status = t.Status.ToString(),
                    ActiveContracts = 0, // Could be calculated if needed
                    MoveInDate = t.MoveInDate,
                    CreatedAt = t.CreatedAt,
                    PropertyId = t.PropertyId,
                    PropertyCode = t.PropertyCode,
                    HasIdentityDocument = false
                })
                .ToListAsync(cancellationToken);

            var tenantIds = tenants.Select(t => t.Id).Distinct().ToList();

            var identityDocTenantIds = await _unitOfWork.Documents.Query()
                .Where(d => !d.IsArchived
                            && d.AssociatedTenantId != null
                            && tenantIds.Contains(d.AssociatedTenantId.Value)
                            && d.Type == DocumentType.PieceIdentite)
                .Select(d => d.AssociatedTenantId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            var identityDocTenantIdSet = identityDocTenantIds.ToHashSet();
            foreach (var t in tenants)
            {
                t.HasIdentityDocument = identityDocTenantIdSet.Contains(t.Id);
            }

            _logger.LogInformation("Found {Count} associated tenants for property {PropertyId}", 
                tenants.Count, propertyId);

            return Result.Success(tenants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving associated tenants for property {PropertyId}", request.PropertyId);
            return Result.Failure<List<TenantDto>>("Error retrieving associated tenants");
        }
    }
}
