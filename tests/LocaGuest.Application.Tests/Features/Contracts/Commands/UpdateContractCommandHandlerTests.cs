using AutoFixture;
using LocaGuest.Application.Features.Contracts.Commands.UpdateContract;
using LocaGuest.Application.Tests.Fixtures;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LocaGuest.Application.Tests.Features.Contracts.Commands;

public class UpdateContractCommandHandlerTests : BaseApplicationTestFixture
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IContractRepository> _contractRepositoryMock;
    private readonly Mock<IPropertyRepository> _propertyRepositoryMock;
    private readonly Mock<IOccupantRepository> _tenantRepositoryMock;
    private readonly Mock<ILogger<UpdateContractCommandHandler>> _loggerMock;
    private readonly UpdateContractCommandHandler _handler;

    public UpdateContractCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _contractRepositoryMock = new Mock<IContractRepository>();
        _propertyRepositoryMock = new Mock<IPropertyRepository>();
        _tenantRepositoryMock = new Mock<IOccupantRepository>();
        _loggerMock = new Mock<ILogger<UpdateContractCommandHandler>>();

        _unitOfWorkMock.Setup(x => x.Contracts).Returns(_contractRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.Properties).Returns(_propertyRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.Occupants).Returns(_tenantRepositoryMock.Object);

        _handler = new UpdateContractCommandHandler(
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithFrenchTypeLabel_NormalizesAndUpdatesSuccessfully()
    {
        var contractId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var OccupantId = Guid.NewGuid();

        var contract = Contract.Create(
            propertyId,
            OccupantId,
            ContractType.Unfurnished,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMonths(12),
            1000m,
            50m,
            deposit: null);

        _contractRepositoryMock
            .Setup(x => x.GetByIdAsync(contractId, It.IsAny<CancellationToken>(), false))
            .ReturnsAsync(contract);

        _propertyRepositoryMock
            .Setup(x => x.GetByIdAsync(propertyId, It.IsAny<CancellationToken>(), false))
            .ReturnsAsync(Fixture.Create<LocaGuest.Domain.Aggregates.PropertyAggregate.Property>());

        _tenantRepositoryMock
            .Setup(x => x.GetByIdAsync(OccupantId, It.IsAny<CancellationToken>(), false))
            .ReturnsAsync(Fixture.Create<LocaGuest.Domain.Aggregates.OccupantAggregate.Occupant>());

        _unitOfWorkMock
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateContractCommand
        {
            ContractId = contractId,
            PropertyId = propertyId,
            PropertyIdIsSet = true,
            OccupantId = OccupantId,
            OccupantIdIsSet = true,
            Type = "Non meublé",
            TypeIsSet = true,
            StartDate = DateTime.UtcNow,
            StartDateIsSet = true,
            EndDate = DateTime.UtcNow.AddMonths(12),
            EndDateIsSet = true,
            Rent = 1200m,
            RentIsSet = true,
            Charges = 0m,
            ChargesIsSet = true,
            Deposit = null,
            DepositIsSet = true,
            RoomId = null,
            RoomIdIsSet = true
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ContractType.Unfurnished, contract.Type);
        Assert.Null(contract.Deposit);

        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDepositNotProvided_DoesNotOverwriteExistingDeposit()
    {
        var contractId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var OccupantId = Guid.NewGuid();

        var contract = Contract.Create(
            propertyId,
            OccupantId,
            ContractType.Furnished,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMonths(12),
            1000m,
            0m,
            deposit: 2000m);

        _contractRepositoryMock
            .Setup(x => x.GetByIdAsync(contractId, It.IsAny<CancellationToken>(), false))
            .ReturnsAsync(contract);

        _propertyRepositoryMock
            .Setup(x => x.GetByIdAsync(propertyId, It.IsAny<CancellationToken>(), false))
            .ReturnsAsync(Fixture.Create<LocaGuest.Domain.Aggregates.PropertyAggregate.Property>());

        _tenantRepositoryMock
            .Setup(x => x.GetByIdAsync(OccupantId, It.IsAny<CancellationToken>(), false))
            .ReturnsAsync(Fixture.Create<LocaGuest.Domain.Aggregates.OccupantAggregate.Occupant>());

        _unitOfWorkMock
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateContractCommand
        {
            ContractId = contractId,
            Rent = 1100m,
            RentIsSet = true
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2000m, contract.Deposit);
    }

    [Fact]
    public async Task Handle_WhenInvalidType_ReturnsFailure()
    {
        var contractId = Guid.NewGuid();
        var propertyId = Guid.NewGuid();
        var OccupantId = Guid.NewGuid();

        var contract = Contract.Create(
            propertyId,
            OccupantId,
            ContractType.Unfurnished,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMonths(12),
            1000m);

        _contractRepositoryMock
            .Setup(x => x.GetByIdAsync(contractId, It.IsAny<CancellationToken>(), false))
            .ReturnsAsync(contract);

        var command = new UpdateContractCommand
        {
            ContractId = contractId,
            Type = "Commercial",
            TypeIsSet = true
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("Invalid contract type", result.ErrorMessage);
    }
}
