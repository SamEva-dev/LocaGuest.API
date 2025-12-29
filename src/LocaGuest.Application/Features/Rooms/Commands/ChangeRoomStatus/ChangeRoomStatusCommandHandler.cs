using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Properties;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Rooms.Commands.ChangeRoomStatus;

public class ChangeRoomStatusCommandHandler : IRequestHandler<ChangeRoomStatusCommand, Result<PropertyRoomDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ChangeRoomStatusCommandHandler> _logger;

    private static bool IsColocation(PropertyUsageType usageType)
        => usageType == PropertyUsageType.ColocationIndividual
           || usageType == PropertyUsageType.Colocation
           || usageType == PropertyUsageType.ColocationSolidaire;

    public ChangeRoomStatusCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<ChangeRoomStatusCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<PropertyRoomDto>> Handle(ChangeRoomStatusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var property = await _unitOfWork.Properties.GetByIdWithRoomsAsync(request.PropertyId, cancellationToken);
            if (property == null)
                return Result.Failure<PropertyRoomDto>("Property not found");

            if (!IsColocation(property.UsageType))
                return Result.Failure<PropertyRoomDto>("Property is not a colocation");

            var room = property.GetRoom(request.RoomId);
            if (room == null)
                return Result.Failure<PropertyRoomDto>("Room not found");

            if (!Enum.TryParse<PropertyRoomStatus>(request.Status, ignoreCase: true, out var targetStatus))
                return Result.Failure<PropertyRoomDto>($"Invalid status '{request.Status}'");

            switch (targetStatus)
            {
                case PropertyRoomStatus.Available:
                    property.ReleaseRoom(request.RoomId);
                    break;

                case PropertyRoomStatus.OnHold:
                    if (!request.ContractId.HasValue)
                        return Result.Failure<PropertyRoomDto>("ContractId is required for OnHold");

                    if (!request.OnHoldUntilUtc.HasValue)
                        return Result.Failure<PropertyRoomDto>("OnHoldUntilUtc is required for OnHold");

                    property.HoldRoom(request.RoomId, request.ContractId.Value, request.OnHoldUntilUtc.Value);
                    break;

                case PropertyRoomStatus.Reserved:
                    if (!request.ContractId.HasValue)
                        return Result.Failure<PropertyRoomDto>("ContractId is required for Reserved");

                    property.ReserveRoom(request.RoomId, request.ContractId.Value);
                    break;

                case PropertyRoomStatus.Occupied:
                    if (!request.ContractId.HasValue)
                        return Result.Failure<PropertyRoomDto>("ContractId is required for Occupied");

                    property.OccupyRoom(request.RoomId, request.ContractId.Value);
                    break;

                default:
                    return Result.Failure<PropertyRoomDto>($"Unsupported status '{targetStatus}'");
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            room = property.GetRoom(request.RoomId);
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

            _logger.LogInformation(
                "Room status changed: {RoomId} for Property {PropertyId} => {Status}",
                room.Id,
                property.Id,
                room.Status);

            return Result.Success(dto);
        }
        catch (Domain.Exceptions.ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error changing room status {RoomId} for property {PropertyId}", request.RoomId, request.PropertyId);
            return Result.Failure<PropertyRoomDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing room status {RoomId} for property {PropertyId}", request.RoomId, request.PropertyId);
            return Result.Failure<PropertyRoomDto>($"Error changing room status: {ex.Message}");
        }
    }
}
