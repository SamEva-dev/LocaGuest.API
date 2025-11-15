using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Tenants;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Tenants.Queries.GetTenant;

public class GetTenantQueryHandler : IRequestHandler<GetTenantQuery, Result<TenantDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetTenantQueryHandler> _logger;

    public GetTenantQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetTenantQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<TenantDto>> Handle(GetTenantQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(request.Id, out var tenantId))
            {
                return Result.Failure<TenantDto>($"Invalid tenant ID format: {request.Id}");
            }

            var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);

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
