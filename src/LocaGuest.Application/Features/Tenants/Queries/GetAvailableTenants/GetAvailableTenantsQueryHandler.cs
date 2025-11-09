using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Tenants;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Tenants.Queries.GetAvailableTenants;

public class GetAvailableTenantsQueryHandler : IRequestHandler<GetAvailableTenantsQuery, Result<List<TenantDto>>>
{
    private readonly ILocaGuestDbContext _context;
    private readonly ILogger<GetAvailableTenantsQueryHandler> _logger;

    public GetAvailableTenantsQueryHandler(
        ILocaGuestDbContext context,
        ILogger<GetAvailableTenantsQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<List<TenantDto>>> Handle(GetAvailableTenantsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var propertyId = Guid.Parse(request.PropertyId);

            // Récupérer les IDs des locataires déjà associés à ce logement via un contrat actif
            var assignedTenantIds = await _context.Contracts
                .Where(c => c.PropertyId == propertyId && c.Status == ContractStatus.Active)
                .Select(c => c.TenantId)
                .Distinct()
                .ToListAsync(cancellationToken);

            // Récupérer tous les locataires sauf ceux déjà assignés
            var availableTenants = await _context.Tenants
                .Where(t => !assignedTenantIds.Contains(t.Id))
                .Select(t => new TenantDto
                {
                    Id = t.Id,
                    FullName = t.FullName,
                    Email = t.Email,
                    Phone = t.Phone,
                    Status = t.Status.ToString(),
                    MoveInDate = t.MoveInDate,
                    CreatedAt = t.CreatedAt
                })
                .OrderBy(t => t.FullName)
                .ToListAsync(cancellationToken);

            return Result.Success(availableTenants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available tenants for property {PropertyId}", request.PropertyId);
            return Result.Failure<List<TenantDto>>("Error retrieving available tenants");
        }
    }
}
