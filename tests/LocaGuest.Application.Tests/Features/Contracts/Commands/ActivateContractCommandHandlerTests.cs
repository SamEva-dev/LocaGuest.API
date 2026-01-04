using FluentAssertions;
using LocaGuest.Application.Features.Contracts.Commands.ActivateContract;
using LocaGuest.Application.Tests.Fixtures;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Aggregates.TenantAggregate;
using LocaGuest.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace LocaGuest.Application.Tests.Features.Contracts.Commands;

public class ActivateContractCommandHandlerTests : BaseApplicationTestFixture
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IContractRepository> _contractRepositoryMock;
    private readonly Mock<IPropertyRepository> _propertyRepositoryMock;
    private readonly Mock<IOccupantRepository> _tenantRepositoryMock;
    private readonly Mock<ILogger<ActivateContractCommandHandler>> _loggerMock;
    private readonly ActivateContractCommandHandler _handler;

    public ActivateContractCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _contractRepositoryMock = new Mock<IContractRepository>();
        _propertyRepositoryMock = new Mock<IPropertyRepository>();
        _tenantRepositoryMock = new Mock<IOccupantRepository>();
        _loggerMock = new Mock<ILogger<ActivateContractCommandHandler>>();

        _unitOfWorkMock.Setup(x => x.Contracts).Returns(_contractRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.Properties).Returns(_propertyRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.Occupants).Returns(_tenantRepositoryMock.Object);

        _unitOfWorkMock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new ActivateContractCommandHandler(_unitOfWorkMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenContractNotFound_ReturnsFailure()
    {
        var command = new ActivateContractCommand(Guid.NewGuid());

        _contractRepositoryMock
            .Setup(x => x.GetByIdAsync(command.ContractId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contract?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Be("Contract not found");
    }

    [Fact]
    public async Task Handle_WhenColocationIndividual_AndRoomIdMissing_ReturnsFailure()
    {
        var contractId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var contract = Contract.Create(propertyId, tenantId, ContractType.Unfurnished, DateTime.UtcNow, DateTime.UtcNow.AddMonths(12), 1000m);
        contract.MarkAsSigned();

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
        property.AddRoom("R1", 400m);

        var tenant = Occupant.Create(fullName: "John Doe", email: "john@doe.com");

        _contractRepositoryMock.Setup(x => x.GetByIdAsync(contractId, It.IsAny<CancellationToken>())).ReturnsAsync(contract);
        _propertyRepositoryMock.Setup(x => x.GetByIdWithRoomsAsync(propertyId, It.IsAny<CancellationToken>())).ReturnsAsync(property);
        _tenantRepositoryMock.Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);

        var result = await _handler.Handle(new ActivateContractCommand(contractId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Be("Pour une colocation individuelle, RoomId est obligatoire.");
    }

    [Fact]
    public async Task Handle_WhenColocationSolidaire_ActivatesAndOccupiesAllRooms()
    {
        var contractId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var contract = Contract.Create(propertyId, tenantId, ContractType.Unfurnished, DateTime.UtcNow, DateTime.UtcNow.AddMonths(12), 1000m);
        contract.MarkAsSigned();

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
        property.AddRoom("R1", 400m);
        property.AddRoom("R2", 400m);

        var tenant = Occupant.Create(fullName: "John Doe", email: "john@doe.com");

        _contractRepositoryMock.Setup(x => x.GetByIdAsync(contractId, It.IsAny<CancellationToken>())).ReturnsAsync(contract);
        _propertyRepositoryMock.Setup(x => x.GetByIdWithRoomsAsync(propertyId, It.IsAny<CancellationToken>())).ReturnsAsync(property);
        _tenantRepositoryMock.Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);

        var result = await _handler.Handle(new ActivateContractCommand(contractId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        tenant.Status.Should().Be(OccupantStatus.Active);
        property.Status.Should().Be(PropertyStatus.Active);
        property.Rooms.All(r => r.Status == PropertyRoomStatus.Occupied).Should().BeTrue();

        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
