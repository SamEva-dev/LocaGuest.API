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
    private readonly IOrganizationContext _orgContext;
    private readonly ILogger<GetPropertyQueryHandler> _logger;

    public GetPropertyQueryHandler(
        ILocaGuestReadDbContext readDb,
        IOrganizationContext orgContext,
        ILogger<GetPropertyQueryHandler> logger)
    {
        _readDb = readDb;
        _orgContext = orgContext;
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

            if (!_orgContext.IsAuthenticated || !_orgContext.OrganizationId.HasValue)
            {
                return Result.Failure<PropertyDetailReadDto>("User not authenticated");
            }

            // ✅ Charger la propriété avec ses rooms (pour les colocations)
            var property = await _readDb.Properties.AsNoTracking()
                .Include(p => p.Rooms)
                .FirstOrDefaultAsync(
                    p => p.Id == propertyId && p.OrganizationId == _orgContext.OrganizationId.Value,
                    cancellationToken);

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
                NightsBookedPerMonth = property.AirbnbSettings.NightsBookedPerMonth,
                CreatedAt = property.CreatedAt,
                UpdatedAt = property.UpdatedAt,
                ImageUrls = property.ImageUrls,
                Rooms = roomDtos,
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
                PurchasePrice = property.PurchasePrice,
                Insurance = property.Insurance,
                ManagementFeesRate = property.ManagementFeesRate,
                MaintenanceRate = property.MaintenanceRate,
                VacancyRate = property.VacancyRate,
                // Administrative info
                CadastralReference = property.CadastralReference,
                LotNumber = property.LotNumber,
                PurchaseDate = property.PurchaseDate,
                TotalWorksAmount = property.TotalWorksAmount,
                EnergyClass = property.EnergyClass,
                ConstructionYear = property.ConstructionYear
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
