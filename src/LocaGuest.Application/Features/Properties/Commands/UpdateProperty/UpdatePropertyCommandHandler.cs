using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Properties;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Properties.Commands.UpdateProperty;

public class UpdatePropertyCommandHandler : IRequestHandler<UpdatePropertyCommand, Result<PropertyDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<UpdatePropertyCommandHandler> _logger;

    public UpdatePropertyCommandHandler(
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ILogger<UpdatePropertyCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<PropertyDetailDto>> Handle(UpdatePropertyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate tenant authentication
            if (!_tenantContext.IsAuthenticated)
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
                zipCode: request.ZipCode,
                country: request.Country,
                surface: request.Surface,
                floor: request.Floor,
                hasElevator: request.HasElevator,
                hasParking: request.HasParking,
                isFurnished: request.IsFurnished,
                charges: request.Charges,
                deposit: request.Deposit,
                notes: request.Notes);

            // Update images if provided
            if (request.ImageUrls != null)
            {
                property.SetImages(request.ImageUrls);
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
                acquisitionDate: request.AcquisitionDate,
                totalWorksAmount: request.TotalWorksAmount);
            
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
                DpeRating = property.DpeRating,
                DpeValue = property.DpeValue,
                GesRating = property.GesRating,
                ElectricDiagnosticDate = property.ElectricDiagnosticDate,
                ElectricDiagnosticExpiry = property.ElectricDiagnosticExpiry,
                GasDiagnosticDate = property.GasDiagnosticDate,
                GasDiagnosticExpiry = property.GasDiagnosticExpiry,
                HasAsbestos = property.HasAsbestos,
                AsbestosDiagnosticDate = property.AsbestosDiagnosticDate,
                ErpZone = property.ErpZone,
                PropertyTax = property.PropertyTax,
                CondominiumCharges = property.CondominiumCharges,
                CadastralReference = property.CadastralReference,
                LotNumber = property.LotNumber,
                AcquisitionDate = property.AcquisitionDate,
                TotalWorksAmount = property.TotalWorksAmount,
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
