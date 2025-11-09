using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Tenants;
using LocaGuest.Domain.Aggregates.TenantAggregate;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Tenants.Queries.GetTenant;

public class GetTenantQueryHandler : IRequestHandler<GetTenantQuery, Result<TenantDto>>
{
    private readonly ILocaGuestDbContext _context;
    private readonly ILogger<GetTenantQueryHandler> _logger;

    public GetTenantQueryHandler(
        ILocaGuestDbContext context,
        ILogger<GetTenantQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<TenantDto>> Handle(GetTenantQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id.ToString() == request.Id, cancellationToken);

            if (tenant == null)
            {
                return Result.Failure<TenantDto>($"Tenant with ID {request.Id} not found");
            }

            var tenantDto = new TenantDto
            {
                Id = tenant.Id,
                FullName = tenant.FullName,
                Email = tenant.Email,
                Phone = tenant.Phone,
                DateOfBirth = null, // Pas dans le domaine
                Status = tenant.Status.ToString(),
                ActiveContracts = 0, // Pas dans le domaine
                MoveInDate = tenant.MoveInDate,
                CreatedAt = tenant.CreatedAt
            };

            return Result.Success(tenantDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant with ID {TenantId}", request.Id);
            return Result.Failure<TenantDto>("Error retrieving tenant");
        }
    }
}
