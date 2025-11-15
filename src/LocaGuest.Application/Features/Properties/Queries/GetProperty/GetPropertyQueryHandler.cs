using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Properties;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Properties.Queries.GetProperty;

public class GetPropertyQueryHandler : IRequestHandler<GetPropertyQuery, Result<PropertyDto>>
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

    public async Task<Result<PropertyDto>> Handle(GetPropertyQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(request.Id, out var propertyId))
            {
                return Result.Failure<PropertyDto>($"Invalid property ID format: {request.Id}");
            }

            var property = await _unitOfWork.Properties.GetByIdAsync(propertyId, cancellationToken);

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
