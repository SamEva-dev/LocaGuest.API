using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Properties;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Rooms.Queries.GetRoom;

public class GetRoomQueryHandler : IRequestHandler<GetRoomQuery, Result<PropertyRoomDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetRoomQueryHandler> _logger;

    public GetRoomQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetRoomQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<PropertyRoomDto>> Handle(GetRoomQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var property = await _unitOfWork.Properties.GetByIdWithRoomsAsync(request.PropertyId, cancellationToken);
            if (property == null)
                return Result.Failure<PropertyRoomDto>("Property not found");

            var room = property.GetRoom(request.RoomId);
            if (room == null)
                return Result.Failure<PropertyRoomDto>("Room not found");

            var dto = new PropertyRoomDto
            {
                Id = room.Id,
                PropertyId = room.PropertyId,
                Name = room.Name,
                Rent = room.Rent,
                Surface = room.Surface,
                Charges = room.Charges,
                Description = room.Description,
                Status = room.Status.ToString()
            };

            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting room {RoomId}", request.RoomId);
            return Result.Failure<PropertyRoomDto>($"Error getting room: {ex.Message}");
        }
    }
}
