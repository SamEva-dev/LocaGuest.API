using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Tenants;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Aggregates.TenantAggregate;
using LocaGuest.Domain.Constants;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Tenants.Commands.CreateTenant;

public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, Result<TenantDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly INumberSequenceService _numberSequenceService;
    private readonly ILogger<CreateTenantCommandHandler> _logger;

    public CreateTenantCommandHandler(
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        INumberSequenceService numberSequenceService,
        ILogger<CreateTenantCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _numberSequenceService = numberSequenceService;
        _logger = logger;
    }

    public async Task<Result<TenantDetailDto>> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_tenantContext.IsAuthenticated)
                return Result.Failure<TenantDetailDto>("User not authenticated");

            // Create full name
            var fullName = $"{request.FirstName} {request.LastName}".Trim();

            // ✅ QUICK WIN: Generate automatic code
            var code = await _numberSequenceService.GenerateNextCodeAsync(
                _tenantContext.TenantId!.Value,
                EntityPrefixes.Tenant,
                cancellationToken);

            _logger.LogInformation("Generated code for new tenant: {Code}", code);

            // Create tenant entity using factory method
            var tenant = Tenant.Create(fullName, request.Email, request.Phone);

            // ✅ Set the generated code
            tenant.SetCode(code);
            
            // ✅ Set detailed information
            tenant.UpdateDetails(
                dateOfBirth: request.DateOfBirth,
                address: request.Address,
                city: request.City,
                postalCode: request.PostalCode,
                country: request.Country,
                nationality: request.Nationality,
                idNumber: request.IdNumber,
                emergencyContact: request.EmergencyContact,
                emergencyPhone: request.EmergencyPhone,
                occupation: request.Occupation,
                monthlyIncome: request.MonthlyIncome,
                notes: request.Notes
            );

            // Add to context
            await _unitOfWork.Tenants.AddAsync(tenant, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Tenant created successfully: {TenantId} - {TenantName}", tenant.Id, tenant.FullName);

            // Map to DTO
            var dto = new TenantDetailDto
            {
                Id = tenant.Id,
                Code = tenant.Code,  // ✅ Include generated code
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
