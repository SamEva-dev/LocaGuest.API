using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Properties;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Constants;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Properties.Commands.CreateProperty;

public class CreatePropertyCommandHandler : IRequestHandler<CreatePropertyCommand, Result<PropertyDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly INumberSequenceService _numberSequenceService;
    private readonly ILogger<CreatePropertyCommandHandler> _logger;

    public CreatePropertyCommandHandler(
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        INumberSequenceService numberSequenceService,
        ILogger<CreatePropertyCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _numberSequenceService = numberSequenceService;
        _logger = logger;
    }

    public async Task<Result<PropertyDetailDto>> Handle(CreatePropertyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate tenant authentication
            if (!_tenantContext.IsAuthenticated)
            {
                _logger.LogWarning("Unauthorized property creation attempt");
                return Result.Failure<PropertyDetailDto>("User not authenticated");
            }

            // Parse PropertyType from string
            if (!Enum.TryParse<PropertyType>(request.Type, ignoreCase: true, out var propertyType))
            {
                return Result.Failure<PropertyDetailDto>("Invalid property type");
            }
            
            // Parse PropertyUsageType from string
            if (!Enum.TryParse<PropertyUsageType>(request.PropertyUsageType, ignoreCase: true, out var usageType))
            {
                return Result.Failure<PropertyDetailDto>("Invalid property usage type");
            }

            // ✅ QUICK WIN: Generate automatic code
            var code = await _numberSequenceService.GenerateNextCodeAsync(
                _tenantContext.TenantId!.Value,
                EntityPrefixes.Property,
                cancellationToken);

            _logger.LogInformation("Generated code for new property: {Code}", code);

            // Create property entity using factory method
            var property = Property.Create(
                request.Name,
                request.Address,
                request.City ?? string.Empty,
                propertyType,
                usageType,
                request.Rent,
                request.Bedrooms ?? 0,
                request.Bathrooms ?? 0,
                request.TotalRooms);

            // ✅ Set the generated code
            property.SetCode(code);
            
            // Configure Airbnb settings if applicable
            if (usageType == PropertyUsageType.Airbnb && 
                request.MinimumStay.HasValue && 
                request.MaximumStay.HasValue && 
                request.PricePerNight.HasValue)
            {
                property.SetAirbnbSettings(
                    request.MinimumStay.Value,
                    request.MaximumStay.Value,
                    request.PricePerNight.Value);
            }

            // Add through repository
            await _unitOfWork.Properties.AddAsync(property, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Property created successfully: {PropertyId} - {PropertyName} for tenant {TenantId}", 
                property.Id, property.Name, _tenantContext.TenantId);

            // Map to DTO
            var dto = new PropertyDetailDto
            {
                Id = property.Id,
                Code = property.Code,  // ✅ Include generated code
                Name = property.Name,
                Address = property.Address,
                City = property.City,
                PostalCode = property.ZipCode,
                Country = property.Country,
                Type = property.Type.ToString(),
                PropertyUsageType = property.UsageType.ToString(),
                Surface = property.Surface ?? 0,
                Bedrooms = property.Bedrooms,
                Bathrooms = property.Bathrooms,
                Floor = property.Floor,
                HasElevator = property.HasElevator,
                HasParking = property.HasParking,
                Rent = property.Rent,
                Charges = property.Charges,
                Status = property.Status.ToString(),
                TotalRooms = property.TotalRooms,
                OccupiedRooms = property.OccupiedRooms,
                MinimumStay = property.MinimumStay,
                MaximumStay = property.MaximumStay,
                PricePerNight = property.PricePerNight,
                CreatedAt = property.CreatedAt
            };

            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating property");
            return Result.Failure<PropertyDetailDto>($"Error creating property: {ex.Message}");
        }
    }
}
