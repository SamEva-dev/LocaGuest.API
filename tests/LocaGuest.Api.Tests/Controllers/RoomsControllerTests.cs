using FluentAssertions;
using LocaGuest.Api.Controllers;
using LocaGuest.Api.Tests.Fixtures;
using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Properties;
using LocaGuest.Application.Features.Rooms.Commands.CreateRoom;
using LocaGuest.Application.Features.Rooms.Commands.UpdateRoom;
using LocaGuest.Application.Features.Rooms.Commands.DeleteRoom;
using LocaGuest.Application.Features.Rooms.Queries.GetPropertyRooms;
using LocaGuest.Application.Features.Rooms.Queries.GetRoom;
using LocaGuest.Application.Features.Rooms.Queries.GetAvailableRooms;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LocaGuest.Api.Tests.Controllers;

public class RoomsControllerTests : BaseTestFixture
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<RoomsController>> _loggerMock;
    private readonly RoomsController _controller;

    public RoomsControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<RoomsController>>();
        _controller = new RoomsController(_mediatorMock.Object, _loggerMock.Object);
    }

    #region GetPropertyRooms Tests

    [Fact]
    public async Task GetPropertyRooms_WithValidPropertyId_ReturnsOkWithRooms()
    {
        // Arrange
        var propertyId = Guid.NewGuid();
        var rooms = new List<PropertyRoomDto>
        {
            new PropertyRoomDto { Id = Guid.NewGuid(), Name = "Room 1", Rent = 500, Status = "Available" },
            new PropertyRoomDto { Id = Guid.NewGuid(), Name = "Room 2", Rent = 600, Status = "Occupied" }
        };

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetPropertyRoomsQuery>(q => q.PropertyId == propertyId), default))
            .ReturnsAsync(Result.Success(rooms));

        // Act
        var result = await _controller.GetPropertyRooms(propertyId) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        result.Value.Should().BeEquivalentTo(rooms);
    }

    [Fact]
    public async Task GetPropertyRooms_WithNonExistingProperty_ReturnsNotFound()
    {
        // Arrange
        var propertyId = Guid.NewGuid();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPropertyRoomsQuery>(), default))
            .ReturnsAsync(Result.Failure<List<PropertyRoomDto>>("Property not found"));

        // Act
        var result = await _controller.GetPropertyRooms(propertyId) as NotFoundObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(404);
    }

    #endregion

    #region GetAvailableRooms Tests

    [Fact]
    public async Task GetAvailableRooms_ReturnsOnlyAvailableRooms()
    {
        // Arrange
        var propertyId = Guid.NewGuid();
        var availableRooms = new List<PropertyRoomDto>
        {
            new PropertyRoomDto { Id = Guid.NewGuid(), Name = "Room 1", Rent = 500, Status = "Available" }
        };

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetAvailableRoomsQuery>(q => q.PropertyId == propertyId), default))
            .ReturnsAsync(Result.Success(availableRooms));

        // Act
        var result = await _controller.GetAvailableRooms(propertyId) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        var rooms = result.Value as List<PropertyRoomDto>;
        rooms.Should().NotBeNull();
        rooms.Should().HaveCount(1);
        rooms![0].Status.Should().Be("Available");
    }

    #endregion

    #region GetRoom Tests

    [Fact]
    public async Task GetRoom_WithValidIds_ReturnsOkWithRoom()
    {
        // Arrange
        var propertyId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var roomDto = new PropertyRoomDto
        {
            Id = roomId,
            PropertyId = propertyId,
            Name = "Room 1",
            Rent = 500,
            Status = "Available"
        };

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetRoomQuery>(q => q.PropertyId == propertyId && q.RoomId == roomId), default))
            .ReturnsAsync(Result.Success(roomDto));

        // Act
        var result = await _controller.GetRoom(propertyId, roomId) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        result.Value.Should().BeEquivalentTo(roomDto);
    }

    [Fact]
    public async Task GetRoom_WithNonExistingRoom_ReturnsNotFound()
    {
        // Arrange
        var propertyId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetRoomQuery>(), default))
            .ReturnsAsync(Result.Failure<PropertyRoomDto>("Room not found"));

        // Act
        var result = await _controller.GetRoom(propertyId, roomId) as NotFoundObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(404);
    }

    #endregion

    #region CreateRoom Tests

    [Fact]
    public async Task CreateRoom_WithValidRequest_ReturnsCreatedWithRoom()
    {
        // Arrange
        var propertyId = Guid.NewGuid();
        var request = new CreateRoomRequest("Room 1", 500, 20, 50, "Test room");
        var roomDto = new PropertyRoomDto
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            Name = "Room 1",
            Rent = 500,
            Status = "Available"
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateRoomCommand>(), default))
            .ReturnsAsync(Result.Success(roomDto));

        // Act
        var result = await _controller.CreateRoom(propertyId, request) as CreatedAtActionResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(201);
        result.Value.Should().BeEquivalentTo(roomDto);
        result.ActionName.Should().Be(nameof(RoomsController.GetRoom));
    }

    [Fact]
    public async Task CreateRoom_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var propertyId = Guid.NewGuid();
        var request = new CreateRoomRequest("", 0, null, 0, null);
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateRoomCommand>(), default))
            .ReturnsAsync(Result.Failure<PropertyRoomDto>("Validation error"));

        // Act
        var result = await _controller.CreateRoom(propertyId, request) as BadRequestObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(400);
    }

    #endregion

    #region UpdateRoom Tests

    [Fact]
    public async Task UpdateRoom_WithValidRequest_ReturnsOkWithUpdatedRoom()
    {
        // Arrange
        var propertyId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var request = new UpdateRoomRequest("Room 1 Updated", 550, 20, 55, "Updated description");
        var roomDto = new PropertyRoomDto
        {
            Id = roomId,
            PropertyId = propertyId,
            Name = "Room 1 Updated",
            Rent = 550,
            Status = "Available"
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<UpdateRoomCommand>(), default))
            .ReturnsAsync(Result.Success(roomDto));

        // Act
        var result = await _controller.UpdateRoom(propertyId, roomId, request) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        result.Value.Should().BeEquivalentTo(roomDto);
    }

    #endregion

    #region DeleteRoom Tests

    [Fact]
    public async Task DeleteRoom_WithAvailableRoom_ReturnsOkWithSuccessMessage()
    {
        // Arrange
        var propertyId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        _mediatorMock
            .Setup(m => m.Send(It.Is<DeleteRoomCommand>(c => c.PropertyId == propertyId && c.RoomId == roomId), default))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _controller.DeleteRoom(propertyId, roomId) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task DeleteRoom_WithOccupiedRoom_ReturnsBadRequest()
    {
        // Arrange
        var propertyId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<DeleteRoomCommand>(), default))
            .ReturnsAsync(Result.Failure("Room is occupied and cannot be deleted"));

        // Act
        var result = await _controller.DeleteRoom(propertyId, roomId) as BadRequestObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(400);
    }

    #endregion
}
