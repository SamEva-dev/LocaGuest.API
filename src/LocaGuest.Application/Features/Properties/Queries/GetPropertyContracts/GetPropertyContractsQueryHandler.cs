using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Properties.Queries.GetPropertyContracts;

public class GetPropertyContractsQueryHandler : IRequestHandler<GetPropertyContractsQuery, Result<List<ContractDto>>>
{
    private readonly ILocaGuestDbContext _context;
    private readonly ILogger<GetPropertyContractsQueryHandler> _logger;

    public GetPropertyContractsQueryHandler(
        ILocaGuestDbContext context,
        ILogger<GetPropertyContractsQueryHandler> _logger)
    {
        _context = context;
        this._logger = _logger;
    }

    public async Task<Result<List<ContractDto>>> Handle(GetPropertyContractsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var propertyId = Guid.Parse(request.PropertyId);

            var contracts = await _context.Contracts
                .Where(c => c.PropertyId == propertyId)
                .OrderByDescending(c => c.StartDate)
                .Select(c => new ContractDto
                {
                    Id = c.Id,
                    PropertyId = c.PropertyId,
                    TenantId = c.TenantId,
                    Type = c.Type.ToString(),
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    Rent = c.Rent,
                    Deposit = c.Deposit,
                    Status = c.Status.ToString(),
                    PaymentsCount = c.Payments.Count,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync(cancellationToken);

            // Charger les noms des locataires
            var tenantIds = contracts.Select(c => c.TenantId).Distinct().ToList();
            var tenants = await _context.Tenants
                .Where(t => tenantIds.Contains(t.Id))
                .Select(t => new { t.Id, t.FullName })
                .ToListAsync(cancellationToken);

            foreach (var contract in contracts)
            {
                contract.TenantName = tenants.FirstOrDefault(t => t.Id == contract.TenantId)?.FullName;
            }

            return Result.Success(contracts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving contracts for property {PropertyId}", request.PropertyId);
            return Result.Failure<List<ContractDto>>("Error retrieving property contracts");
        }
    }
}
