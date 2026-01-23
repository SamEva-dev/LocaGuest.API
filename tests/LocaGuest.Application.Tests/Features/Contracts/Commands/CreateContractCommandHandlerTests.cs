using AutoFixture;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Features.Contracts.Commands.CreateContract;
using LocaGuest.Application.Services;
using LocaGuest.Application.Tests.Fixtures;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Aggregates.OccupantAggregate;
using LocaGuest.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LocaGuest.Application.Tests.Features.Contracts.Commands;

public class CreateContractCommandHandlerTests : BaseApplicationTestFixture
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IContractRepository> _contractRepositoryMock;
    private readonly Mock<IPropertyRepository> _propertyRepositoryMock;
    private readonly Mock<IOccupantRepository> _tenantRepositoryMock;
    private readonly Mock<IOrganizationContext> _orgContextMock;
    private readonly Mock<INumberSequenceService> _numberSequenceServiceMock;
    private readonly Mock<ILogger<CreateContractCommandHandler>> _loggerMock;
    private readonly CreateContractCommandHandler _handler;

    public CreateContractCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _contractRepositoryMock = new Mock<IContractRepository>();
        _propertyRepositoryMock = new Mock<IPropertyRepository>();
        _tenantRepositoryMock = new Mock<IOccupantRepository>();
        _orgContextMock = new Mock<IOrganizationContext>();
        _loggerMock = new Mock<ILogger<CreateContractCommandHandler>>();

        _unitOfWorkMock.Setup(x => x.Contracts).Returns(_contractRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.Properties).Returns(_propertyRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.Occupants).Returns(_tenantRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.Deposits).Returns(new Mock<IDepositRepository>().Object);
        _orgContextMock.Setup(x => x.IsAuthenticated).Returns(true);
        _orgContextMock.Setup(x => x.OrganizationId).Returns(Guid.NewGuid());
        _numberSequenceServiceMock = new Mock<INumberSequenceService>();
        _numberSequenceServiceMock.Setup(x => x.GenerateNextCodeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("CTR-001");

        _handler = new CreateContractCommandHandler(
            _unitOfWorkMock.Object,
            _orgContextMock.Object,
            _numberSequenceServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesContractSuccessfully()
    {
        // Arrange
        var property = Property.Create(
            name: "Test Property",
            address: "1 Test Street",
            city: "Test City",
            type: PropertyType.Apartment,
            usageType: PropertyUsageType.Complete,
            rent: 1500m,
            bedrooms: 2,
            bathrooms: 1);
        property.SetOrganizationId(Guid.NewGuid());

        var tenant = Occupant.Create(fullName: "Test Tenant", email: "tenant@test.com");

        var command = new CreateContractCommand
        {
            PropertyId = property.Id,
            OccupantId = tenant.Id,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddYears(1),
            Rent = 1500m,
            Deposit = 3000m
        };

        _propertyRepositoryMock
            .Setup(x => x.GetByIdWithRoomsAsync(property.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(property);

        _tenantRepositoryMock
            .Setup(x => x.GetByIdAsync(tenant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _unitOfWorkMock
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        
        _unitOfWorkMock.Verify(
            x => x.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ReturnsFailure()
    {
        // Arrange
        _orgContextMock.Setup(x => x.IsAuthenticated).Returns(false);

        var command = new CreateContractCommand
        {
            PropertyId = Guid.NewGuid(),
            OccupantId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            Rent = 1500m
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
    }
}
