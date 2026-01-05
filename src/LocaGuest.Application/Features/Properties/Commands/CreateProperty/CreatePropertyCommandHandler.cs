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
    private readonly IOrganizationContext _orgContext;
    private readonly INumberSequenceService _numberSequenceService;
    private readonly ILogger<CreatePropertyCommandHandler> _logger;

    public CreatePropertyCommandHandler(
        IUnitOfWork unitOfWork,
        IOrganizationContext orgContext,
        INumberSequenceService numberSequenceService,
        ILogger<CreatePropertyCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _orgContext = orgContext;
        _numberSequenceService = numberSequenceService;
        _logger = logger;
    }

    public async Task<Result<PropertyDetailDto>> Handle(CreatePropertyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate tenant authentication
            if (!_orgContext.IsAuthenticated)
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
                _orgContext.OrganizationId!.Value,
                EntityPrefixes.Property,
                cancellationToken);

            _logger.LogInformation("Generated code for new property: {Code}", code);

            // Create property entity using factory method
            var property = Property.Create(
                request.Name,
                request.Address,
                request.City,
                propertyType,
                usageType,
                request.Rent,
                request.Bedrooms ?? 0,
                request.Bathrooms ?? 0,
                request.TotalRooms);

            // Set the generated code
            property.SetCode(code);
            
            // Update extended details (city, postalCode, country, surface, floor, elevator, parking, furnished, charges, deposit, notes)
            property.UpdateExtendedDetails(
                city: request.City,
                postalCode: request.PostalCode,
                country: request.Country,
                surface: request.Surface,
                floor: request.Floor,
                hasElevator: request.HasElevator,
                hasParking: request.HasParking,
                hasBalcony: request.HasBalcony,
                isFurnished: request.IsFurnished,
                charges: request.Charges,
                deposit: request.Deposit,
                description: request.Description,
                energyClass: request.EnergyClass,
                constructionYear: request.ConstructionYear);
            
            // Update diagnostics if provided
            if (request.DpeRating != null || request.GesRating != null)
            {
                property.UpdateDiagnostics(
                    dpeRating: request.DpeRating,
                    dpeValue: request.DpeValue,
                    gesRating: request.GesRating,
                    electricDiagnosticDate: request.ElectricDiagnosticDate,
                    electricDiagnosticExpiry: request.ElectricDiagnosticExpiry,
                    gasDiagnosticDate: request.GasDiagnosticDate,
                    gasDiagnosticExpiry: request.GasDiagnosticExpiry,
                    hasAsbestos: request.HasAsbestos,
                    asbestosDiagnosticDate: request.AsbestosDiagnosticDate,
                    erpZone: request.ErpZone);
            }
            
            // Update financial info if provided
            if (request.PropertyTax.HasValue || request.CondominiumCharges.HasValue)
            {
                property.UpdateFinancialInfo(
                    propertyTax: request.PropertyTax,
                    condominiumCharges: request.CondominiumCharges);
            }

            // Purchase + rentability fields
            if (request.PurchasePrice.HasValue)
            {
                property.UpdatePurchaseInfo(purchasePrice: request.PurchasePrice);
            }

            if (request.Insurance.HasValue || request.ManagementFeesRate.HasValue || request.MaintenanceRate.HasValue || request.VacancyRate.HasValue || request.NightsBookedPerMonth.HasValue)
            {
                property.UpdateRentabilityInfo(
                    insurance: request.Insurance,
                    managementFeesRate: request.ManagementFeesRate,
                    maintenanceRate: request.MaintenanceRate,
                    vacancyRate: request.VacancyRate,
                    nightsBookedPerMonth: request.NightsBookedPerMonth);
            }
            
            // Update administrative info if provided
            if (!string.IsNullOrEmpty(request.CadastralReference) || request.PurchaseDate.HasValue)
            {
                property.UpdateAdministrativeInfo(
                    cadastralReference: request.CadastralReference,
                    lotNumber: request.LotNumber,
                    purchaseDate: request.PurchaseDate,
                    totalWorksAmount: request.TotalWorksAmount);
            }
            
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
            
            // Create rooms for colocation if provided
            if ((
                usageType == PropertyUsageType.Colocation || 
                usageType == PropertyUsageType.ColocationIndividual) && 
                request.Rooms != null && request.Rooms.Any())
            {
                foreach (var roomDto in request.Rooms)
                {
                    property.AddRoom(
                        name: roomDto.Name,
                        rent: roomDto.Rent,
                        surface: roomDto.Surface,
                        charges: roomDto.Charges,
                        description: roomDto.Description);
                }
                
                _logger.LogInformation("{RoomCount} rooms added to colocation property {PropertyId}", 
                    request.Rooms.Count, property.Id);
            }

            // Add through repository
            await _unitOfWork.Properties.AddAsync(property, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Property created successfully: {PropertyId} - {PropertyName} for tenant {TenantId}", 
                property.Id, property.Name, _orgContext.OrganizationId);

            // Map to DTO with ALL fields
            var dto = new PropertyDetailDto
            {
                Id = property.Id,
                Code = property.Code,
                Name = property.Name,
                Address = property.Address,
                City = property.City,
                PostalCode = property.PostalCode,
                Country = property.Country,
                Type = property.Type.ToString(),
                PropertyUsageType = property.UsageType.ToString(),
                Surface = property.Surface ?? 0,
                Bedrooms = property.Bedrooms,
                Bathrooms = property.Bathrooms,
                Floor = property.Floor,
                HasElevator = property.HasElevator,
                HasParking = property.HasParking,
                HasBalcony = property.HasBalcony,
                Rent = property.Rent,
                Charges = property.Charges,
                Status = property.Status.ToString(),
                TotalRooms = property.TotalRooms,
                OccupiedRooms = property.OccupiedRooms,
                MinimumStay = property.AirbnbSettings.MinimumStay,
                MaximumStay = property.AirbnbSettings.MaximumStay,
                PricePerNight = property.AirbnbSettings.PricePerNight,
                // Diagnostics
                DpeRating = property.Diagnostics.DpeRating,
                DpeValue = property.Diagnostics.DpeValue,
                GesRating = property.Diagnostics.GesRating,
                ElectricDiagnosticDate = property.Diagnostics.ElectricDiagnosticDate,
                ElectricDiagnosticExpiry = property.Diagnostics.ElectricDiagnosticExpiry,
                GasDiagnosticDate = property.Diagnostics.GasDiagnosticDate,
                GasDiagnosticExpiry = property.Diagnostics.GasDiagnosticExpiry,
                HasAsbestos = property.Diagnostics.HasAsbestos,
                AsbestosDiagnosticDate = property.Diagnostics.AsbestosDiagnosticDate,
                ErpZone = property.Diagnostics.ErpZone,
                // Financial info
                PropertyTax = property.PropertyTax,
                CondominiumCharges = property.CondominiumCharges,
                Insurance = property.Insurance,
                ManagementFeesRate = property.ManagementFeesRate,
                MaintenanceRate = property.MaintenanceRate,
                VacancyRate = property.VacancyRate,
                NightsBookedPerMonth = property.AirbnbSettings.NightsBookedPerMonth,
                // Administrative info
                CadastralReference = property.CadastralReference,
                LotNumber = property.LotNumber,
                PurchaseDate = property.PurchaseDate,
               PurchasePrice= property.PurchasePrice,
                TotalWorksAmount = property.TotalWorksAmount,
                // Timestamps
                CreatedAt = property.CreatedAt,
                ImageUrls = property.ImageUrls,
                UpdatedAt = property.UpdatedAt,
                // Other fields
                Description = property.Description,
                EnergyClass = property.EnergyClass,
                ConstructionYear = property.ConstructionYear,
                // Rooms (for colocation)
                Rooms = property.Rooms.Select(r => new PropertyRoomDto
                {
                    Id = r.Id,
                    PropertyId = r.PropertyId,
                    Name = r.Name,
                    Surface = r.Surface,
                    Rent = r.Rent,
                    Charges = r.Charges,
                    Description = r.Description,
                    Status = r.Status.ToString(),
                    CurrentContractId = r.CurrentContractId
                }).ToList()
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
