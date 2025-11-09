using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Contracts;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Contracts.Queries.GetAllContracts;

public class GetAllContractsQueryHandler : IRequestHandler<GetAllContractsQuery, Result<List<ContractDto>>>
{
    private readonly ILocaGuestDbContext _context;
    private readonly ILogger<GetAllContractsQueryHandler> _logger;

    public GetAllContractsQueryHandler(
        ILocaGuestDbContext context,
        ILogger<GetAllContractsQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<List<ContractDto>>> Handle(GetAllContractsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = _context.Contracts.AsQueryable();

            // Filtrer par statut
            if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<ContractStatus>(request.Status, out var status))
            {
                query = query.Where(c => c.Status == status);
            }

            // Filtrer par type
            if (!string.IsNullOrEmpty(request.Type) && Enum.TryParse<ContractType>(request.Type, out var type))
            {
                query = query.Where(c => c.Type == type);
            }

            var contracts = await query
                .OrderByDescending(c => c.StartDate)
                .ToListAsync(cancellationToken);

            // Charger les propriétés et locataires
            var propertyIds = contracts.Select(c => c.PropertyId).Distinct().ToList();
            var tenantIds = contracts.Select(c => c.TenantId).Distinct().ToList();

            var properties = await _context.Properties
                .Where(p => propertyIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

            var tenants = await _context.Tenants
                .Where(t => tenantIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.FullName, cancellationToken);

            var result = contracts.Select(c => new ContractDto
            {
                Id = c.Id,
                PropertyId = c.PropertyId,
                TenantId = c.TenantId,
                PropertyName = properties.GetValueOrDefault(c.PropertyId),
                TenantName = tenants.GetValueOrDefault(c.TenantId),
                Type = c.Type.ToString(),
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Rent = c.Rent,
                Deposit = c.Deposit,
                Status = c.Status.ToString(),
                PaymentsCount = c.Payments.Count,
                CreatedAt = c.CreatedAt
            }).ToList();

            // Filtrer par terme de recherche après chargement des noms
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var searchLower = request.SearchTerm.ToLower();
                result = result.Where(c =>
                    (c.PropertyName?.ToLower().Contains(searchLower) ?? false) ||
                    (c.TenantName?.ToLower().Contains(searchLower) ?? false)
                ).ToList();
            }

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving contracts");
            return Result.Failure<List<ContractDto>>("Error retrieving contracts");
        }
    }
}
