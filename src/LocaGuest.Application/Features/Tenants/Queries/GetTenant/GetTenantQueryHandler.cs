using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Tenants;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Tenants.Queries.GetTenant;

public class GetTenantQueryHandler : IRequestHandler<GetTenantQuery, Result<TenantDetailDto>>
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

    public async Task<Result<TenantDetailDto>> Handle(GetTenantQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(request.Id, out var tenantId))
            {
                return Result.Failure<TenantDetailDto>($"Invalid tenant ID format: {request.Id}");
            }

            var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);

            if (tenant == null)
            {
                return Result.Failure<TenantDetailDto>($"Tenant with ID {request.Id} not found");
            }

            // ✅ Map to TenantDetailDto with all fields
            var tenantDto = new TenantDetailDto
            {
                Id = tenant.Id,
                Code = tenant.Code,
                FullName = tenant.FullName,
                Email = tenant.Email,
                Phone = tenant.Phone,
                DateOfBirth = tenant.DateOfBirth,
                Status = tenant.Status.ToString(),
                ActiveContracts = 0, // TODO: Calculate from contracts
                MoveInDate = tenant.MoveInDate,
                CreatedAt = tenant.CreatedAt,
                PropertyId = tenant.PropertyId,
                PropertyCode = tenant.PropertyCode,
                
                // ✅ Detailed information
                Address = tenant.Address,
                City = tenant.City,
                PostalCode = tenant.PostalCode,
                Country = tenant.Country,
                Nationality = tenant.Nationality,
                IdNumber = tenant.IdNumber,
                EmergencyContact = tenant.EmergencyContact,
                EmergencyPhone = tenant.EmergencyPhone,
                Occupation = tenant.Occupation,
                MonthlyIncome = tenant.MonthlyIncome,
                Notes = tenant.Notes
            };

            return Result.Success(tenantDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant with ID {TenantId}", request.Id);
            return Result.Failure<TenantDetailDto>("Error retrieving tenant");
        }
    }
}
