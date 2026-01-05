using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Properties;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Properties.Queries.GetProperty;

public class GetPropertyQueryHandler : IRequestHandler<GetPropertyQuery, Result<PropertyDetailReadDto>>
{
    private readonly ILocaGuestReadDbContext _readDb;
    private readonly ILogger<GetPropertyQueryHandler> _logger;

    public GetPropertyQueryHandler(
        ILocaGuestReadDbContext readDb,
        ILogger<GetPropertyQueryHandler> logger)
    {
        _readDb = readDb;
        _logger = logger;
    }

    public async Task<Result<PropertyDetailReadDto>> Handle(GetPropertyQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(request.Id, out var propertyId))
            {
                return Result.Failure<PropertyDetailReadDto>($"Invalid property ID format: {request.Id}");
            }

            // ✅ Charger la propriété avec ses rooms (pour les colocations)
            var property = await _readDb.Properties.AsNoTracking()
                .Include(p => p.Rooms)
                .FirstOrDefaultAsync(p => p.Id == propertyId, cancellationToken);

            if (property == null)
            {
                return Result.Failure<PropertyDetailReadDto>($"Property with ID {request.Id} not found");
            }

            // ✅ Mapper les rooms pour les colocations
            var roomDtos = property.Rooms?.Select(r => new PropertyRoomDto
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
            }).ToList() ?? new List<PropertyRoomDto>();

            var propertyDto = new PropertyDetailReadDto
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
                CreatedAt = property.CreatedAt,
                UpdatedAt = property.UpdatedAt,
                ImageUrls = property.ImageUrls,
                Rooms = roomDtos  // Inclure les rooms
            };

            return Result.Success(propertyDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving property with ID {PropertyId}", request.Id);
            return Result.Failure<PropertyDetailReadDto>("Error retrieving property");
        }
    }
}
