using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Properties;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Properties.Queries.GetProperty;

public class GetPropertyQueryHandler : IRequestHandler<GetPropertyQuery, Result<PropertyDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetPropertyQueryHandler> _logger;

    public GetPropertyQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetPropertyQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<PropertyDetailDto>> Handle(GetPropertyQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(request.Id, out var propertyId))
            {
                return Result.Failure<PropertyDetailDto>($"Invalid property ID format: {request.Id}");
            }

            // ✅ Charger la propriété avec ses rooms (pour les colocations)
            var property = await _unitOfWork.Properties.Query()
                .Include(p => p.Rooms)
                .FirstOrDefaultAsync(p => p.Id == propertyId, cancellationToken);

            if (property == null)
            {
                return Result.Failure<PropertyDetailDto>($"Property with ID {request.Id} not found");
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

            var propertyDto = new PropertyDetailDto
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
                HasBalcony = false, // Pas dans le domaine
                Rent = property.Rent,
                Charges = property.Charges,
                Status = property.Status.ToString(),
                TotalRooms = property.TotalRooms,
                OccupiedRooms = property.OccupiedRooms,
                ReservedRooms = property.ReservedRooms,
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
                PurchasePrice = property.PurchasePrice,
                Insurance = property.Insurance,
                ManagementFeesRate = property.ManagementFeesRate,
                MaintenanceRate = property.MaintenanceRate,
                VacancyRate = property.VacancyRate,
                NightsBookedPerMonth = property.NightsBookedPerMonth,
                CadastralReference = property.CadastralReference,
                LotNumber = property.LotNumber,
                AcquisitionDate = property.AcquisitionDate,
                TotalWorksAmount = property.TotalWorksAmount,
                CreatedAt = property.CreatedAt,
                UpdatedAt = property.UpdatedAt,
                ImageUrls = property.ImageUrls,
                Rooms = roomDtos,  // ✅ Inclure les rooms
                Description = property.Notes,
                PurchaseDate = property.AcquisitionDate
            };

            return Result.Success(propertyDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving property with ID {PropertyId}", request.Id);
            return Result.Failure<PropertyDetailDto>("Error retrieving property");
        }
    }
}
