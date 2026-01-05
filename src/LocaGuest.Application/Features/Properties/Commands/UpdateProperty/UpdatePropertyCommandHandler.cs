using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Properties;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Properties.Commands.UpdateProperty;

public class UpdatePropertyCommandHandler : IRequestHandler<UpdatePropertyCommand, Result<PropertyDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdatePropertyCommandHandler> _logger;

    public UpdatePropertyCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<UpdatePropertyCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<PropertyDetailDto>> Handle(UpdatePropertyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate tenant authentication
            if (!_currentUserService.IsAuthenticated)
            {
                _logger.LogWarning("Unauthorized property update attempt");
                return Result.Failure<PropertyDetailDto>("User not authenticated");
            }

            // Get existing property
            var property = await _unitOfWork.Properties.GetByIdAsync(request.Id, cancellationToken);
            
            if (property == null)
            {
                return Result.Failure<PropertyDetailDto>($"Property with ID {request.Id} not found");
            }

            // Update type / usage type / capacity (if provided)
            if (!string.IsNullOrWhiteSpace(request.Type) && Enum.TryParse<PropertyType>(request.Type, true, out var parsedType))
            {
                property.UpdateClassification(type: parsedType);
            }

            if (!string.IsNullOrWhiteSpace(request.PropertyUsageType) && Enum.TryParse<PropertyUsageType>(request.PropertyUsageType, true, out var parsedUsage))
            {
                property.UpdateClassification(usageType: parsedUsage);
            }

            if (request.TotalRooms.HasValue)
            {
                property.UpdateCapacity(totalRooms: request.TotalRooms);
            }

            // Airbnb settings
            if (request.MinimumStay.HasValue || request.MaximumStay.HasValue || request.PricePerNight.HasValue)
            {
                property.UpdateAirbnbSettings(
                    minimumStay: request.MinimumStay,
                    maximumStay: request.MaximumStay,
                    pricePerNight: request.PricePerNight);
            }

            // Update basic details
            property.UpdateDetails(
                name: request.Name,
                address: request.Address,
                rent: request.Rent,
                bedrooms: request.Bedrooms,
                bathrooms: request.Bathrooms);

            // Update extended details
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

            // Update images if provided
            if (request.ImageUrls != null)
            {
                property.UpdateImageUrls(request.ImageUrls);
            }
            
            // Update diagnostics if provided
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
            
            // Update financial info if provided
            property.UpdateFinancialInfo(
                propertyTax: request.PropertyTax,
                condominiumCharges: request.CondominiumCharges);
            
            // Update administrative info if provided
            property.UpdateAdministrativeInfo(
                cadastralReference: request.CadastralReference,
                lotNumber: request.LotNumber,
                purchaseDate: request.PurchaseDate,
                totalWorksAmount: request.TotalWorksAmount);

            // ✅ Purchase + rentability fields
            property.UpdatePurchaseInfo(purchasePrice: request.PurchasePrice);
            property.UpdateRentabilityInfo(
                insurance: request.Insurance,
                managementFeesRate: request.ManagementFeesRate,
                maintenanceRate: request.MaintenanceRate,
                vacancyRate: request.VacancyRate,
                nightsBookedPerMonth: request.NightsBookedPerMonth);
            
            // ✅ Update rooms for colocation if provided
            if (request.Rooms != null)
            {
                // Get existing rooms
                var existingRooms = property.Rooms.ToList();
                var newRoomNames = request.Rooms.Select(r => r.Name).ToList();
                
                // Remove rooms that are not in the new list (only if Available)
                foreach (var existingRoom in existingRooms)
                {
                    if (!newRoomNames.Contains(existingRoom.Name))
                    {
                        try
                        {
                            property.RemoveRoom(existingRoom.Id);
                            _logger.LogInformation("Room {RoomName} removed from property {PropertyId}", 
                                existingRoom.Name, property.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("Cannot remove room {RoomName}: {Message}", 
                                existingRoom.Name, ex.Message);
                            // Continue - room is probably occupied
                        }
                    }
                }
                
                // Update or add rooms
                foreach (var roomDto in request.Rooms)
                {
                    var existingRoom = existingRooms.FirstOrDefault(r => r.Name == roomDto.Name);
                    
                    if (existingRoom != null)
                    {
                        // Update existing room
                        property.UpdateRoom(
                            roomId: existingRoom.Id,
                            name: roomDto.Name,
                            rent: roomDto.Rent,
                            surface: roomDto.Surface,
                            charges: roomDto.Charges,
                            description: roomDto.Description);
                        
                        _logger.LogInformation("Room {RoomName} updated in property {PropertyId}", 
                            roomDto.Name, property.Id);
                    }
                    else
                    {
                        // Add new room
                        property.AddRoom(
                            name: roomDto.Name,
                            rent: roomDto.Rent,
                            surface: roomDto.Surface,
                            charges: roomDto.Charges ?? 0,
                            description: roomDto.Description);
                        
                        _logger.LogInformation("Room {RoomName} added to property {PropertyId}", 
                            roomDto.Name, property.Id);
                    }
                }
            }

            // Update through repository
            _unitOfWork.Properties.Update(property);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Property updated successfully: {PropertyId} - {PropertyName}", 
                property.Id, property.Name);

            // Map to DTO
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
                IsFurnished = property.IsFurnished,
                Rent = property.Rent,
                Charges = property.Charges,
                Deposit = property.Deposit,
                Description = property.Description,
                Status = property.Status.ToString(),
                TotalRooms = property.TotalRooms,
                OccupiedRooms = property.OccupiedRooms,
                ReservedRooms = property.ReservedRooms,
                MinimumStay = property.AirbnbSettings.MinimumStay,
                MaximumStay = property.AirbnbSettings.MaximumStay,
                PricePerNight = property.AirbnbSettings.PricePerNight,
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
                PropertyTax = property.PropertyTax,
                CondominiumCharges = property.CondominiumCharges,
                PurchasePrice = property.PurchasePrice,
                Insurance = property.Insurance,
                ManagementFeesRate = property.ManagementFeesRate,
                MaintenanceRate = property.MaintenanceRate,
                VacancyRate = property.VacancyRate,
                NightsBookedPerMonth = property.AirbnbSettings.NightsBookedPerMonth,
                CadastralReference = property.CadastralReference,
                LotNumber = property.LotNumber,
                PurchaseDate = property.PurchaseDate,
                TotalWorksAmount = property.TotalWorksAmount,
                EnergyClass = property.EnergyClass,
                ConstructionYear = property.ConstructionYear,
                CreatedAt = property.CreatedAt,
                UpdatedAt = property.UpdatedAt,
                // ✅ Include rooms in response
                Rooms = property.Rooms.Select(r => new PropertyRoomDto
                {
                    Id = r.Id,
                    PropertyId = r.PropertyId,
                    Name = r.Name,
                    Rent = r.Rent,
                    Surface = r.Surface,
                    Charges = r.Charges,
                    Description = r.Description,
                    Status = r.Status.ToString()
                }).ToList()
            };

            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating property {PropertyId}", request.Id);
            return Result.Failure<PropertyDetailDto>($"Error updating property: {ex.Message}");
        }
    }
}
