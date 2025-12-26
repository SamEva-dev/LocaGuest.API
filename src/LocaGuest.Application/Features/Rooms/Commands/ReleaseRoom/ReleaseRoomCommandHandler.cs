using LocaGuest.Application.Common;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Rooms.Commands.ReleaseRoom;

public class ReleaseRoomCommandHandler : IRequestHandler<ReleaseRoomCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReleaseRoomCommandHandler> _logger;

    public ReleaseRoomCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<ReleaseRoomCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(ReleaseRoomCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var property = await _unitOfWork.Properties.GetByIdWithRoomsAsync(request.PropertyId, cancellationToken);
            if (property == null)
                return Result.Failure("Property not found");

            if (property.UsageType != PropertyUsageType.ColocationIndividual && property.UsageType != PropertyUsageType.Colocation)
                return Result.Failure("Property is not a colocation");

            var room = property.GetRoom(request.RoomId);
            if (room == null)
                return Result.Failure("Room not found");

            property.ReleaseRoom(request.RoomId);

            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Room released: {RoomId} for Property {PropertyId}", request.RoomId, request.PropertyId);
            return Result.Success();
        }
        catch (Domain.Exceptions.ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error releasing room {RoomId} for property {PropertyId}", request.RoomId, request.PropertyId);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing room {RoomId} for property {PropertyId}", request.RoomId, request.PropertyId);
            return Result.Failure($"Error releasing room: {ex.Message}");
        }
    }
}
