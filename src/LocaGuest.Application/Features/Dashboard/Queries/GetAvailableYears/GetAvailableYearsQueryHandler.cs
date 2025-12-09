using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Dashboard.Queries.GetAvailableYears;

public class GetAvailableYearsQueryHandler : IRequestHandler<GetAvailableYearsQuery, Result<AvailableYearsDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<GetAvailableYearsQueryHandler> _logger;

    public GetAvailableYearsQueryHandler(
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ILogger<GetAvailableYearsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<AvailableYearsDto>> Handle(GetAvailableYearsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_tenantContext.IsAuthenticated)
                return Result.Failure<AvailableYearsDto>("User not authenticated");

            var currentYear = DateTime.UtcNow.Year;
            var years = new List<int>();

            // Récupérer les années des contrats et paiements
            var contracts = await _unitOfWork.Contracts.GetAllAsync(cancellationToken);
            var payments = await _unitOfWork.Payments.GetAllAsync(cancellationToken);

            var contractYears = contracts
                .SelectMany(c => new[] { c.StartDate.Year, c.EndDate.Year })
                .Distinct();

            var paymentYears = payments
                .Select(p => p.ExpectedDate.Year)
                .Distinct();

            // Combiner toutes les années et ajouter l'année courante
            years = contractYears
                .Union(paymentYears)
                .Union(new[] { currentYear })
                .OrderByDescending(y => y)
                .ToList();

            // Si aucune donnée, retourner au moins l'année courante
            if (!years.Any())
            {
                years.Add(currentYear);
            }

            _logger.LogInformation("Retrieved {Count} available years for tenant {TenantId}", years.Count, _tenantContext.TenantId);

            return Result.Success(new AvailableYearsDto
            {
                Years = years
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available years for tenant {TenantId}", _tenantContext.TenantId);
            return Result.Failure<AvailableYearsDto>($"Error retrieving available years: {ex.Message}");
        }
    }
}
