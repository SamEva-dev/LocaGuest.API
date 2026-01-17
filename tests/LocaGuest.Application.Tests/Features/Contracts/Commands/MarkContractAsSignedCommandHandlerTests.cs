using FluentAssertions;
using LocaGuest.Application.Features.Contracts.Commands.MarkContractAsSigned;
using LocaGuest.Application.Tests.Fixtures;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace LocaGuest.Application.Tests.Features.Contracts.Commands;

public class MarkContractAsSignedCommandHandlerTests : BaseApplicationTestFixture
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IContractRepository> _contractRepositoryMock;
    private readonly Mock<IPropertyRepository> _propertyRepositoryMock;
    private readonly Mock<ILogger<MarkContractAsSignedCommandHandler>> _loggerMock;
    private readonly MarkContractAsSignedCommandHandler _handler;

    public MarkContractAsSignedCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _contractRepositoryMock = new Mock<IContractRepository>();
        _propertyRepositoryMock = new Mock<IPropertyRepository>();
        _loggerMock = new Mock<ILogger<MarkContractAsSignedCommandHandler>>();

        _unitOfWorkMock.Setup(x => x.Contracts).Returns(_contractRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.Properties).Returns(_propertyRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new MarkContractAsSignedCommandHandler(_unitOfWorkMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenContractNotFound_ReturnsFailure()
    {
        var command = new MarkContractAsSignedCommand { ContractId = Guid.NewGuid() };
        _contractRepositoryMock.Setup(x => x.GetByIdAsync(command.ContractId, It.IsAny<CancellationToken>())).ReturnsAsync((Contract?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Be("Contract not found");
    }

    [Fact]
    public async Task Handle_WhenPropertyNotFound_ReturnsFailure()
    {
        var contractId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var OccupantId = Guid.NewGuid();

        var contract = Contract.Create(propertyId, OccupantId, ContractType.Unfurnished, DateTime.UtcNow, DateTime.UtcNow.AddMonths(12), 1000m);
        _contractRepositoryMock.Setup(x => x.GetByIdAsync(contractId, It.IsAny<CancellationToken>())).ReturnsAsync(contract);
        _propertyRepositoryMock.Setup(x => x.GetByIdWithRoomsAsync(propertyId, It.IsAny<CancellationToken>())).ReturnsAsync((Property?)null);

        var result = await _handler.Handle(new MarkContractAsSignedCommand { ContractId = contractId }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Be("Property not found");
    }

    [Fact]
    public async Task Handle_WhenColocationIndividual_AndRoomIdMissing_ReturnsFailure()
    {
        var contractId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var OccupantId = Guid.NewGuid();

        var contract = Contract.Create(propertyId, OccupantId, ContractType.Unfurnished, DateTime.UtcNow, DateTime.UtcNow.AddMonths(12), 1000m);

        var property = Property.Create(
            name: "Test",
            address: "Addr",
            city: "City",
            type: PropertyType.Apartment,
            usageType: PropertyUsageType.ColocationIndividual,
            rent: 1000m,
            bedrooms: 3,
            bathrooms: 1,
            totalRooms: 1);
        property.SetOrganizationId(Guid.NewGuid());
        property.AddRoom("R1", 400m);

        _contractRepositoryMock.Setup(x => x.GetByIdAsync(contractId, It.IsAny<CancellationToken>())).ReturnsAsync(contract);
        _propertyRepositoryMock.Setup(x => x.GetByIdWithRoomsAsync(propertyId, It.IsAny<CancellationToken>())).ReturnsAsync(property);

        var result = await _handler.Handle(new MarkContractAsSignedCommand { ContractId = contractId }, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Be("Pour une colocation individuelle, RoomId est obligatoire.");
    }

    [Fact]
    public async Task Handle_WhenColocationSolidaire_ReservesAllRooms()
    {
        var contractId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var OccupantId = Guid.NewGuid();

        var contract = Contract.Create(propertyId, OccupantId, ContractType.Unfurnished, DateTime.UtcNow, DateTime.UtcNow.AddMonths(12), 1000m);

        var property = Property.Create(
            name: "Test",
            address: "Addr",
            city: "City",
            type: PropertyType.Apartment,
            usageType: PropertyUsageType.ColocationSolidaire,
            rent: 1000m,
            bedrooms: 3,
            bathrooms: 1,
            totalRooms: 2);
        property.SetOrganizationId(Guid.NewGuid());
        property.AddRoom("R1", 400m);
        property.AddRoom("R2", 400m);

        _contractRepositoryMock.Setup(x => x.GetByIdAsync(contractId, It.IsAny<CancellationToken>())).ReturnsAsync(contract);
        _propertyRepositoryMock.Setup(x => x.GetByIdWithRoomsAsync(propertyId, It.IsAny<CancellationToken>())).ReturnsAsync(property);

        var result = await _handler.Handle(new MarkContractAsSignedCommand { ContractId = contractId }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        contract.Status.Should().Be(ContractStatus.Signed);
        property.Status.Should().Be(PropertyStatus.Reserved);
        property.Rooms.All(r => r.Status == PropertyRoomStatus.Reserved).Should().BeTrue();

        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
