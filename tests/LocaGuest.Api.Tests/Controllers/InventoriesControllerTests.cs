using FluentAssertions;
using LocaGuest.Api.Controllers;
using LocaGuest.Api.Tests.Fixtures;
using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Inventories;
using LocaGuest.Application.Features.Inventories.Commands.CreateInventoryEntry;
using LocaGuest.Application.Features.Inventories.Commands.CreateInventoryExit;
using LocaGuest.Application.Features.Inventories.Queries.GetInventoryEntry;
using LocaGuest.Application.Features.Inventories.Queries.GetInventoryByContract;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LocaGuest.Api.Tests.Controllers;

public class InventoriesControllerTests : BaseTestFixture
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<InventoriesController>> _loggerMock;
    private readonly InventoriesController _controller;

    public InventoriesControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<InventoriesController>>();
        _controller = new InventoriesController(_mediatorMock.Object, _loggerMock.Object);
    }

    #region CreateEntry Tests

    [Fact]
    public async Task CreateEntry_WithValidCommand_ReturnsOkWithInventory()
    {
        // Arrange
        var command = new CreateInventoryEntryCommand
        {
            PropertyId = Guid.NewGuid(),
            ContractId = Guid.NewGuid(),
            InspectionDate = DateTime.UtcNow,
            AgentName = "Agent Test",
            TenantPresent = true
        };

        var inventoryDto = new InventoryEntryDto
        {
            Id = Guid.NewGuid(),
            PropertyId = command.PropertyId,
            ContractId = command.ContractId,
            AgentName = command.AgentName,
            Status = "Draft"
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateInventoryEntryCommand>(), default))
            .ReturnsAsync(Result.Success(inventoryDto));

        // Act
        var result = await _controller.CreateEntry(command) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        result.Value.Should().BeEquivalentTo(inventoryDto);

        _mediatorMock.Verify(m => m.Send(
            It.Is<CreateInventoryEntryCommand>(c => c.PropertyId == command.PropertyId),
            default), Times.Once);
    }

    [Fact]
    public async Task CreateEntry_WithFailedResult_ReturnsBadRequest()
    {
        // Arrange
        var command = new CreateInventoryEntryCommand();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateInventoryEntryCommand>(), default))
            .ReturnsAsync(Result.Failure<InventoryEntryDto>("Validation error"));

        // Act
        var result = await _controller.CreateEntry(command) as BadRequestObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(400);
    }

    #endregion

    #region CreateExit Tests

    [Fact]
    public async Task CreateExit_WithValidCommand_ReturnsOkWithInventory()
    {
        // Arrange
        var command = new CreateInventoryExitCommand
        {
            PropertyId = Guid.NewGuid(),
            ContractId = Guid.NewGuid(),
            InventoryEntryId = Guid.NewGuid(),
            InspectionDate = DateTime.UtcNow,
            AgentName = "Agent Test",
            TenantPresent = true
        };

        var inventoryDto = new InventoryExitDto
        {
            Id = Guid.NewGuid(),
            PropertyId = command.PropertyId,
            ContractId = command.ContractId,
            InventoryEntryId = command.InventoryEntryId,
            Status = "Draft",
            TotalDeductionAmount = 0
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateInventoryExitCommand>(), default))
            .ReturnsAsync(Result.Success(inventoryDto));

        // Act
        var result = await _controller.CreateExit(command) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        result.Value.Should().BeEquivalentTo(inventoryDto);
    }

    #endregion

    #region GetEntry Tests

    [Fact]
    public async Task GetEntry_WithExistingId_ReturnsOkWithInventory()
    {
        // Arrange
        var inventoryId = Guid.NewGuid();
        var inventoryDto = new InventoryEntryDto
        {
            Id = inventoryId,
            PropertyId = Guid.NewGuid(),
            ContractId = Guid.NewGuid(),
            AgentName = "Agent Test",
            Status = "Completed"
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetInventoryEntryQuery>(), default))
            .ReturnsAsync(Result.Success(inventoryDto));

        // Act
        var result = await _controller.GetEntry(inventoryId) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        result.Value.Should().BeEquivalentTo(inventoryDto);
    }

    [Fact]
    public async Task GetEntry_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var inventoryId = Guid.NewGuid();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetInventoryEntryQuery>(), default))
            .ReturnsAsync(Result.Failure<InventoryEntryDto>("Inventory not found"));

        // Act
        var result = await _controller.GetEntry(inventoryId) as NotFoundObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(404);
    }

    #endregion

    #region GetByContract Tests

    [Fact]
    public async Task GetByContract_WithValidContractId_ReturnsOk()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var contractInventories = new ContractInventoriesDto
        {
            Entry = new InventoryEntryDto { Id = Guid.NewGuid() },
            Exit = null
        };

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetInventoryByContractQuery>(q => q.ContractId == contractId), default))
            .ReturnsAsync(Result.Success(contractInventories));

        // Act
        var result = await _controller.GetByContract(contractId) as OkObjectResult;

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        result.Value.Should().BeEquivalentTo(contractInventories);
    }

    #endregion
}
