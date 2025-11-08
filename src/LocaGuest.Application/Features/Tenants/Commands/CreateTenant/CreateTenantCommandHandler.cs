using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Tenants;
using LocaGuest.Domain.Aggregates.TenantAggregate;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Tenants.Commands.CreateTenant;

public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, Result<TenantDetailDto>>
{
    private readonly ILocaGuestDbContext _context;
    private readonly ILogger<CreateTenantCommandHandler> _logger;

    public CreateTenantCommandHandler(
        ILocaGuestDbContext context,
        ILogger<CreateTenantCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<TenantDetailDto>> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Create full name
            var fullName = $"{request.FirstName} {request.LastName}".Trim();

            // Create tenant entity using factory method
            var tenant = Tenant.Create(fullName, request.Email, request.Phone);

            // Add to context
            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Tenant created successfully: {TenantId} - {TenantName}", tenant.Id, tenant.FullName);

            // Map to DTO
            var dto = new TenantDetailDto
            {
                Id = tenant.Id,
                FullName = tenant.FullName,
                Email = tenant.Email,
                Phone = tenant.Phone,
                Status = tenant.Status.ToString(),
                ActiveContracts = 0,
                MoveInDate = tenant.MoveInDate,
                CreatedAt = tenant.CreatedAt,
                Address = request.Address,
                City = request.City,
                PostalCode = request.PostalCode,
                Country = request.Country,
                Nationality = request.Nationality,
                IdNumber = request.IdNumber,
                EmergencyContact = request.EmergencyContact,
                EmergencyPhone = request.EmergencyPhone,
                Occupation = request.Occupation,
                MonthlyIncome = request.MonthlyIncome,
                Notes = request.Notes,
                DateOfBirth = request.DateOfBirth
            };

            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant");
            return Result.Failure<TenantDetailDto>($"Error creating tenant: {ex.Message}");
        }
    }
}
