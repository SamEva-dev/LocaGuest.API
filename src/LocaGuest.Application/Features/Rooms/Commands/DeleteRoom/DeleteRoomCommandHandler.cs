using LocaGuest.Application.Common;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Rooms.Commands.DeleteRoom;

public class DeleteRoomCommandHandler : IRequestHandler<DeleteRoomCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteRoomCommandHandler> _logger;

    public DeleteRoomCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteRoomCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteRoomCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Vérifier que la propriété existe
            var property = await _unitOfWork.Properties.GetByIdAsync(request.PropertyId, cancellationToken);
            if (property == null)
                return Result.Failure("Property not found");

            // Supprimer la chambre (validera que la chambre est disponible)
            property.RemoveRoom(request.RoomId);

            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Room deleted: {RoomId} from Property {PropertyId}",
                request.RoomId, request.PropertyId);

            return Result.Success();
        }
        catch (Domain.Exceptions.ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error deleting room {RoomId}", request.RoomId);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting room {RoomId}", request.RoomId);
            return Result.Failure($"Error deleting room: {ex.Message}");
        }
    }
}
