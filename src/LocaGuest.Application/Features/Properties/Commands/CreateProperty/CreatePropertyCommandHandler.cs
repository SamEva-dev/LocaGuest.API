using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Properties;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Properties.Commands.CreateProperty;

public class CreatePropertyCommandHandler : IRequestHandler<CreatePropertyCommand, Result<PropertyDetailDto>>
{
    private readonly ILocaGuestDbContext _context;
    private readonly ILogger<CreatePropertyCommandHandler> _logger;

    public CreatePropertyCommandHandler(
        ILocaGuestDbContext context,
        ILogger<CreatePropertyCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<PropertyDetailDto>> Handle(CreatePropertyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Parse PropertyType from string
            if (!Enum.TryParse<PropertyType>(request.Type, ignoreCase: true, out var propertyType))
            {
                return Result.Failure<PropertyDetailDto>("Invalid property type");
            }

            // Create property entity using factory method
            var property = Property.Create(
                request.Name,
                request.Address,
                request.City ?? string.Empty,
                propertyType,
                request.Rent,
                request.Bedrooms ?? 0,
                request.Bathrooms ?? 0);

            // Add to context
            _context.Properties.Add(property);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Property created successfully: {PropertyId} - {PropertyName}", property.Id, property.Name);

            // Map to DTO
            var dto = new PropertyDetailDto
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
                Rent = property.Rent,
                Charges = property.Charges,
                Status = property.Status.ToString(),
                CreatedAt = property.CreatedAt
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
