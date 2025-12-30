using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Models;
using LocaGuest.Application.DTOs.Properties;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Properties.Queries.GetProperties;

public class GetPropertiesQueryHandler : IRequestHandler<GetPropertiesQuery, Result<PagedResult<PropertyDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetPropertiesQueryHandler> _logger;

    public GetPropertiesQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetPropertiesQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<PagedResult<PropertyDto>>> Handle(GetPropertiesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // ✅ Charger les rooms pour les colocations
            var queryWithRooms = _unitOfWork.Properties.Query().Include(p => p.Rooms);
            IQueryable<Property> query = queryWithRooms;

            // Apply filters
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var searchLower = request.Search.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(searchLower) ||
                    p.Address.ToLower().Contains(searchLower) ||
                    p.City.ToLower().Contains(searchLower));
            }

            if (!string.IsNullOrWhiteSpace(request.Status) &&
                Enum.TryParse<PropertyStatus>(request.Status, ignoreCase: true, out var status))
            {
                query = query.Where(p => p.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(request.Type) &&
                Enum.TryParse<PropertyType>(request.Type, ignoreCase: true, out var type))
            {
                query = query.Where(p => p.Type == type);
            }

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var properties = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // Map to DTOs
            var dtos = properties.Select(p => new PropertyDto
            {
                Id = p.Id,
                Name = p.Name,
                Address = p.Address,
                City = p.City,
                PostalCode = p.PostalCode,
                Country = p.Country,
                Type = p.Type.ToString(),
                Surface = p.Surface ?? 0,
                Bedrooms = p.Bedrooms,
                Bathrooms = p.Bathrooms,
                Floor = p.Floor,
                HasElevator = p.HasElevator,
                HasParking = p.HasParking,
                HasBalcony = p.HasBalcony,
                Rent = p.Rent,
                Charges = p.Charges,
                Description = p.Description,
                EnergyClass = p.EnergyClass,
                ConstructionYear = p.ConstructionYear,
                TotalRooms = p.TotalRooms,
                OccupiedRooms = p.OccupiedRooms,
                ReservedRooms = p.ReservedRooms,
                Status = p.Status.ToString(),
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                PurchaseDate = p.PurchaseDate,
                PurchasePrice= p.PurchasePrice,
                AsbestosDiagnosticDate = p.AsbestosDiagnosticDate,
                CadastralReference = p.CadastralReference,
                Code = p.Code,
                DpeRating = p.DpeRating,
                DpeValue = p.DpeValue,
                ElectricDiagnosticDate = p.ElectricDiagnosticDate,
                ElectricDiagnosticExpiry = p.ElectricDiagnosticExpiry,
                GasDiagnosticDate = p.GasDiagnosticDate,
                GasDiagnosticExpiry = p.GasDiagnosticExpiry,
                HasAsbestos = p.HasAsbestos,
                ErpZone = p.ErpZone,
                PropertyTax = p.PropertyTax,
                CondominiumCharges = p.CondominiumCharges,
                LotNumber = p.LotNumber,
                TotalWorksAmount = p.TotalWorksAmount,
                MinimumStay = p.MinimumStay,
                MaximumStay = p.MaximumStay,
                PricePerNight = p.PricePerNight,
                GesRating = p.GesRating,
                PropertyUsageType = p.UsageType.ToString(),
                ImageUrls = p.ImageUrls,
                Rooms = p.Rooms?.Select(r => new PropertyRoomDto  // ✅ Mapper les rooms
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
                }).ToList() ?? new List<PropertyRoomDto>()

            }).ToList();

            var result = new PagedResult<PropertyDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving properties");
            return Result.Failure<PagedResult<PropertyDto>>($"Error retrieving properties: {ex.Message}");
        }
    }
}
