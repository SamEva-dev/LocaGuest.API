using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Common.Models;
using LocaGuest.Application.DTOs.Properties;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Properties.Queries.GetProperties;

public class GetPropertiesQueryHandler : IRequestHandler<GetPropertiesQuery, Result<PagedResult<PropertyDto>>>
{
    private readonly ILocaGuestDbContext _context;
    private readonly ILogger<GetPropertiesQueryHandler> _logger;

    public GetPropertiesQueryHandler(
        ILocaGuestDbContext context,
        ILogger<GetPropertiesQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<PagedResult<PropertyDto>>> Handle(GetPropertiesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = _context.Properties.AsQueryable();

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
                PostalCode = p.ZipCode,
                Country = p.Country,
                Type = p.Type.ToString(),
                Surface = p.Surface ?? 0,
                Bedrooms = p.Bedrooms,
                Bathrooms = p.Bathrooms,
                Floor = p.Floor,
                HasElevator = p.HasElevator,
                HasParking = p.HasParking,
                Rent = p.Rent,
                Charges = p.Charges,
                Status = p.Status.ToString(),
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
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
