using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Properties;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Rooms.Queries.GetPropertyRooms;

public class GetPropertyRoomsQueryHandler : IRequestHandler<GetPropertyRoomsQuery, Result<List<PropertyRoomDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetPropertyRoomsQueryHandler> _logger;

    public GetPropertyRoomsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetPropertyRoomsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<List<PropertyRoomDto>>> Handle(GetPropertyRoomsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var property = await _unitOfWork.Properties.GetByIdWithRoomsAsync(request.PropertyId, cancellationToken);
            if (property == null)
                return Result.Failure<List<PropertyRoomDto>>("Property not found");

            var rooms = property.Rooms.Select(r => new PropertyRoomDto
            {
                Id = r.Id,
                PropertyId = r.PropertyId,
                Name = r.Name,
                Rent = r.Rent,
                Surface = r.Surface,
                Charges = r.Charges,
                Description = r.Description,
                Status = r.Status.ToString()
            }).ToList();

            return Result.Success(rooms);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rooms for property {PropertyId}", request.PropertyId);
            return Result.Failure<List<PropertyRoomDto>>($"Error getting rooms: {ex.Message}");
        }
    }
}
