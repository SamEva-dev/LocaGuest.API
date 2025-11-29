using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Properties;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Rooms.Queries.GetAvailableRooms;

public class GetAvailableRoomsQueryHandler : IRequestHandler<GetAvailableRoomsQuery, Result<List<PropertyRoomDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetAvailableRoomsQueryHandler> _logger;

    public GetAvailableRoomsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetAvailableRoomsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<List<PropertyRoomDto>>> Handle(GetAvailableRoomsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var property = await _unitOfWork.Properties.GetByIdAsync(request.PropertyId, cancellationToken);
            if (property == null)
                return Result.Failure<List<PropertyRoomDto>>("Property not found");

            var availableRooms = property.GetAvailableRooms().Select(r => new PropertyRoomDto
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

            return Result.Success(availableRooms);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available rooms for property {PropertyId}", request.PropertyId);
            return Result.Failure<List<PropertyRoomDto>>($"Error getting available rooms: {ex.Message}");
        }
    }
}
