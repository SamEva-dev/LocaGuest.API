using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Properties;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Rooms.Commands.CreateRoom;

public class CreateRoomCommandHandler : IRequestHandler<CreateRoomCommand, Result<PropertyRoomDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateRoomCommandHandler> _logger;

    public CreateRoomCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<CreateRoomCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<PropertyRoomDto>> Handle(CreateRoomCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Vérifier que la propriété existe
            var property = await _unitOfWork.Properties.GetByIdAsync(request.PropertyId, cancellationToken);
            if (property == null)
                return Result.Failure<PropertyRoomDto>("Property not found");

            // Vérifier que c'est une colocation
            if (property.UsageType != Domain.Aggregates.PropertyAggregate.PropertyUsageType.Colocation &&
                property.UsageType != Domain.Aggregates.PropertyAggregate.PropertyUsageType.ColocationIndividual)
            {
                return Result.Failure<PropertyRoomDto>("Only colocation properties can have rooms");
            }

            // Ajouter la chambre
            property.AddRoom(
                name: request.Name,
                rent: request.Rent,
                surface: request.Surface,
                charges: request.Charges,
                description: request.Description);

            await _unitOfWork.CommitAsync(cancellationToken);

            // Récupérer la chambre créée
            var room = property.Rooms.FirstOrDefault(r => r.Name == request.Name);
            if (room == null)
                return Result.Failure<PropertyRoomDto>("Failed to create room");

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

            _logger.LogInformation("Room created: {RoomId} - {RoomName} for Property {PropertyId}",
                room.Id, room.Name, request.PropertyId);

            return Result.Success(dto);
        }
        catch (Domain.Exceptions.ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error creating room");
            return Result.Failure<PropertyRoomDto>(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating room for property {PropertyId}", request.PropertyId);
            return Result.Failure<PropertyRoomDto>($"Error creating room: {ex.Message}");
        }
    }
}
