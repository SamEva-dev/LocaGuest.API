using LocaGuest.Application.Features.Rooms.Commands.CreateRoom;
using LocaGuest.Application.Features.Rooms.Commands.UpdateRoom;
using LocaGuest.Application.Features.Rooms.Commands.DeleteRoom;
using LocaGuest.Application.Features.Rooms.Commands.ChangeRoomStatus;
using LocaGuest.Application.Features.Rooms.Commands.ReleaseRoom;
using LocaGuest.Application.Features.Rooms.Queries.GetPropertyRooms;
using LocaGuest.Application.Features.Rooms.Queries.GetRoom;
using LocaGuest.Application.Features.Rooms.Queries.GetAvailableRooms;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaGuest.Api.Controllers;

/// <summary>
/// Controller pour la gestion des chambres (Rooms) dans les colocations
/// Implémentation 100% CQRS - Aucune logique métier dans le controller
/// </summary>
[Authorize]
[ApiController]
[Route("api/properties/{propertyId:guid}/rooms")]
public class RoomsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<RoomsController> _logger;

    public RoomsController(
        IMediator mediator,
        ILogger<RoomsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Récupérer toutes les chambres d'une propriété
    /// GET /api/properties/{propertyId}/rooms
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPropertyRooms(Guid propertyId)
    {
        var query = new GetPropertyRoomsQuery(propertyId);
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
            return NotFound(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Récupérer uniquement les chambres disponibles
    /// GET /api/properties/{propertyId}/rooms/available
    /// </summary>
    [HttpGet("available")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvailableRooms(Guid propertyId)
    {
        var query = new GetAvailableRoomsQuery(propertyId);
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
            return NotFound(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Récupérer une chambre spécifique
    /// GET /api/properties/{propertyId}/rooms/{roomId}
    /// </summary>
    [HttpGet("{roomId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoom(Guid propertyId, Guid roomId)
    {
        var query = new GetRoomQuery(propertyId, roomId);
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
            return NotFound(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Créer une nouvelle chambre
    /// POST /api/properties/{propertyId}/rooms
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateRoom(Guid propertyId, [FromBody] CreateRoomRequest request)
    {
        var command = new CreateRoomCommand
        {
            PropertyId = propertyId,
            Name = request.Name,
            Rent = request.Rent,
            Surface = request.Surface,
            Charges = request.Charges,
            Description = request.Description
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.ErrorMessage?.Contains("not found") == true)
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Room created: {RoomId} - {RoomName}", result.Data!.Id, result.Data.Name);

        return CreatedAtAction(
            nameof(GetRoom),
            new { propertyId, roomId = result.Data.Id },
            result.Data);
    }

    /// <summary>
    /// Mettre à jour une chambre
    /// PUT /api/properties/{propertyId}/rooms/{roomId}
    /// </summary>
    [HttpPut("{roomId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRoom(
        Guid propertyId,
        Guid roomId,
        [FromBody] UpdateRoomRequest request)
    {
        var command = new UpdateRoomCommand
        {
            PropertyId = propertyId,
            RoomId = roomId,
            Name = request.Name,
            Rent = request.Rent,
            Surface = request.Surface,
            Charges = request.Charges,
            Description = request.Description
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.ErrorMessage?.Contains("not found") == true)
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Room updated: {RoomId} - {RoomName}", roomId, result.Data!.Name);

        return Ok(result.Data);
    }

    /// <summary>
    /// Supprimer une chambre (uniquement si disponible)
    /// DELETE /api/properties/{propertyId}/rooms/{roomId}
    /// </summary>
    [HttpDelete("{roomId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRoom(Guid propertyId, Guid roomId)
    {
        var command = new DeleteRoomCommand(propertyId, roomId);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.ErrorMessage?.Contains("not found") == true)
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Room deleted: {RoomId} from Property {PropertyId}", roomId, propertyId);

        return Ok(new { message = "Room deleted successfully" });
    }

    /// <summary>
    /// Libérer une chambre (retour à Available)
    /// POST /api/properties/{propertyId}/rooms/{roomId}/release
    /// </summary>
    [HttpPost("{roomId:guid}/release")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReleaseRoom(Guid propertyId, Guid roomId)
    {
        var command = new ReleaseRoomCommand(propertyId, roomId);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.ErrorMessage?.Contains("not found") == true)
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        _logger.LogInformation("Room released: {RoomId} from Property {PropertyId}", roomId, propertyId);
        return Ok(new { message = "Room released successfully" });
    }

    /// <summary>
    /// Changer le statut d'une chambre
    /// POST /api/properties/{propertyId}/rooms/{roomId}/status
    /// </summary>
    [HttpPost("{roomId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeRoomStatus(
        Guid propertyId,
        Guid roomId,
        [FromBody] ChangeRoomStatusRequest request)
    {
        var command = new ChangeRoomStatusCommand
        {
            PropertyId = propertyId,
            RoomId = roomId,
            Status = request.Status,
            ContractId = request.ContractId,
            OnHoldUntilUtc = request.OnHoldUntilUtc
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.ErrorMessage?.Contains("not found") == true)
                return NotFound(new { message = result.ErrorMessage });
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Data);
    }
}

/// <summary>
/// Request pour créer une chambre
/// </summary>
public record CreateRoomRequest(
    string Name,
    decimal Rent,
    decimal? Surface,
    decimal Charges,
    string? Description
);

/// <summary>
/// Request pour mettre à jour une chambre
/// </summary>
public record UpdateRoomRequest(
    string? Name,
    decimal? Rent,
    decimal? Surface,
    decimal? Charges,
    string? Description
);

/// <summary>
/// Request pour changer le statut d'une chambre
/// </summary>
public record ChangeRoomStatusRequest(
    string Status,
    Guid? ContractId,
    DateTime? OnHoldUntilUtc
);