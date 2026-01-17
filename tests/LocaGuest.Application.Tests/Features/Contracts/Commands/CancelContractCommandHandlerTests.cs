using FluentAssertions;
using LocaGuest.Application.Features.Contracts.Commands.CancelContract;
using LocaGuest.Application.Tests.Fixtures;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Aggregates.OccupantAggregate;
using LocaGuest.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace LocaGuest.Application.Tests.Features.Contracts.Commands;

public class CancelContractCommandHandlerTests : BaseApplicationTestFixture
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IContractRepository> _contractRepositoryMock;
    private readonly Mock<IPropertyRepository> _propertyRepositoryMock;
    private readonly Mock<IOccupantRepository> _tenantRepositoryMock;
    private readonly Mock<ILogger<CancelContractCommandHandler>> _loggerMock;
    private readonly CancelContractCommandHandler _handler;

    public CancelContractCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _contractRepositoryMock = new Mock<IContractRepository>();
        _propertyRepositoryMock = new Mock<IPropertyRepository>();
        _tenantRepositoryMock = new Mock<IOccupantRepository>();
        _loggerMock = new Mock<ILogger<CancelContractCommandHandler>>();

        _unitOfWorkMock.Setup(x => x.Contracts).Returns(_contractRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.Properties).Returns(_propertyRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.Occupants).Returns(_tenantRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new CancelContractCommandHandler(_unitOfWorkMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenContractNotFound_ReturnsFailure()
    {
        var command = new CancelContractCommand(Guid.NewGuid());
        _contractRepositoryMock.Setup(x => x.GetByIdAsync(command.ContractId, It.IsAny<CancellationToken>())).ReturnsAsync((Contract?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Be("Contract not found");
    }

    [Fact]
    public async Task Handle_WhenNotSigned_ReturnsFailure()
    {
        var contractId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var OccupantId = Guid.NewGuid();

        var contract = Contract.Create(propertyId, OccupantId, ContractType.Unfurnished, DateTime.UtcNow, DateTime.UtcNow.AddMonths(12), 1000m);
        // still Draft

        _contractRepositoryMock.Setup(x => x.GetByIdAsync(contractId, It.IsAny<CancellationToken>())).ReturnsAsync(contract);

        var result = await _handler.Handle(new CancelContractCommand(contractId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Be("Only Signed contracts can be cancelled");
    }

    [Fact]
    public async Task Handle_WhenColocationSolidaire_ReleasesAllRooms_AndUpdatesTenant()
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
        property.ReserveAllRooms(contract.Id);

        var tenant = Occupant.Create(fullName: "John Doe", email: "john@doe.com");
        tenant.SetReserved();

        _contractRepositoryMock.Setup(x => x.GetByIdAsync(contractId, It.IsAny<CancellationToken>())).ReturnsAsync(contract);
        _propertyRepositoryMock.Setup(x => x.GetByIdWithRoomsAsync(propertyId, It.IsAny<CancellationToken>())).ReturnsAsync(property);
        _tenantRepositoryMock.Setup(x => x.GetByIdAsync(OccupantId, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);

        _contractRepositoryMock.Setup(x => x.GetByTenantIdAsync(tenant.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Contract>());
        _contractRepositoryMock.Setup(x => x.GetByPropertyIdAsync(property.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Contract>());

        var result = await _handler.Handle(new CancelContractCommand(contractId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        property.Status.Should().Be(PropertyStatus.Vacant);
        property.Rooms.All(r => r.Status == PropertyRoomStatus.Available).Should().BeTrue();
        tenant.Status.Should().Be(OccupantStatus.Inactive);

        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
