using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Properties;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Rooms.Commands.UpdateRoom;

public class UpdateRoomCommandHandler : IRequestHandler<UpdateRoomCommand, Result<PropertyRoomDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateRoomCommandHandler> _logger;

    public UpdateRoomCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<UpdateRoomCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<PropertyRoomDto>> Handle(UpdateRoomCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Vérifier que la propriété existe
            var property = await _unitOfWork.Properties.GetByIdWithRoomsAsync(request.PropertyId, cancellationToken);
            if (property == null)
                return Result.Failure<PropertyRoomDto>("Property not found");

            // Mettre à jour la chambre
            property.UpdateRoom(
                roomId: request.RoomId,
                name: request.Name,
                rent: request.Rent,
                surface: request.Surface,
                charges: request.Charges,
                description: request.Description);

            await _unitOfWork.CommitAsync(cancellationToken);

            // Récupérer la chambre mise à jour
            var room = property.Rooms.FirstOrDefault(r => r.Id == request.RoomId);
            if (room == null)
                return Result.Failure<PropertyRoomDto>("Room not found after update");

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

            _logger.LogInformation("Room updated: {RoomId} - {RoomName}", room.Id, room.Name);

            return Result.Success(dto);
        }
        catch (Domain.Exceptions.ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error updating room {RoomId}", request.RoomId);
            return Result.Failure<PropertyRoomDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating room {RoomId}", request.RoomId);
            return Result.Failure<PropertyRoomDto>($"Error updating room: {ex.Message}");
        }
    }
}
