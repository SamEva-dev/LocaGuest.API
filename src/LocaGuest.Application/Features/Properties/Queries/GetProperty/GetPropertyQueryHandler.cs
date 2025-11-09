using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Properties;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Properties.Queries.GetProperty;

public class GetPropertyQueryHandler : IRequestHandler<GetPropertyQuery, Result<PropertyDto>>
{
    private readonly ILocaGuestDbContext _context;
    private readonly ILogger<GetPropertyQueryHandler> _logger;

    public GetPropertyQueryHandler(
        ILocaGuestDbContext context,
        ILogger<GetPropertyQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<PropertyDto>> Handle(GetPropertyQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var property = await _context.Properties
                .FirstOrDefaultAsync(p => p.Id.ToString() == request.Id, cancellationToken);

            if (property == null)
            {
                return Result.Failure<PropertyDto>($"Property with ID {request.Id} not found");
            }

            var propertyDto = new PropertyDto
            {
                Id = property.Id,
                Name = property.Name,
                Address = property.Address,
                City = property.City,
                PostalCode = property.ZipCode,
                Country = property.Country,
                Type = property.Type.ToString(),
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
                CreatedAt = property.CreatedAt,
                UpdatedAt = property.UpdatedAt
            };

            return Result.Success(propertyDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving property with ID {PropertyId}", request.Id);
            return Result.Failure<PropertyDto>("Error retrieving property");
        }
    }
}
