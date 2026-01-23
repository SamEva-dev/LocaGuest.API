using LocaGuest.Application.Features.Contracts.Commands.ActivateContract;
using LocaGuest.Application.Tests.Fixtures;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Aggregates.OccupantAggregate;
using LocaGuest.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

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

        Assert.True(result.IsFailure);
        Assert.Equal("Contract not found", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_WhenColocationIndividual_AndRoomIdMissing_ReturnsFailure()
    {
        var contractId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var OccupantId = Guid.NewGuid();

        var contract = Contract.Create(propertyId, OccupantId, ContractType.Unfurnished, DateTime.UtcNow, DateTime.UtcNow.AddMonths(12), 1000m);
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
        property.SetOrganizationId(Guid.NewGuid());
        property.AddRoom("R1", 400m);

        var tenant = Occupant.Create(fullName: "John Doe", email: "john@doe.com");

        _contractRepositoryMock.Setup(x => x.GetByIdAsync(contractId, It.IsAny<CancellationToken>())).ReturnsAsync(contract);
        _propertyRepositoryMock.Setup(x => x.GetByIdWithRoomsAsync(propertyId, It.IsAny<CancellationToken>())).ReturnsAsync(property);
        _tenantRepositoryMock.Setup(x => x.GetByIdAsync(OccupantId, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);

        var result = await _handler.Handle(new ActivateContractCommand(contractId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Pour une colocation individuelle, RoomId est obligatoire.", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_WhenColocationSolidaire_ActivatesAndOccupiesAllRooms()
    {
        var contractId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var OccupantId = Guid.NewGuid();

        var contract = Contract.Create(propertyId, OccupantId, ContractType.Unfurnished, DateTime.UtcNow, DateTime.UtcNow.AddMonths(12), 1000m);
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
        property.SetOrganizationId(Guid.NewGuid());
        property.AddRoom("R1", 400m);
        property.AddRoom("R2", 400m);

        var tenant = Occupant.Create(fullName: "John Doe", email: "john@doe.com");

        _contractRepositoryMock.Setup(x => x.GetByIdAsync(contractId, It.IsAny<CancellationToken>())).ReturnsAsync(contract);
        _propertyRepositoryMock.Setup(x => x.GetByIdWithRoomsAsync(propertyId, It.IsAny<CancellationToken>())).ReturnsAsync(property);
        _tenantRepositoryMock.Setup(x => x.GetByIdAsync(OccupantId, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);

        var result = await _handler.Handle(new ActivateContractCommand(contractId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(OccupantStatus.Active, tenant.Status);
        Assert.Equal(PropertyStatus.Active, property.Status);
        Assert.True(property.Rooms.All(r => r.Status == PropertyRoomStatus.Occupied));

        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
