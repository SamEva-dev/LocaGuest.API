using AutoFixture;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Features.Properties.Commands.CreateProperty;
using LocaGuest.Application.Services;
using LocaGuest.Application.Tests.Fixtures;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LocaGuest.Application.Tests.Features.Properties.Commands;

public class CreatePropertyCommandHandlerTests : BaseApplicationTestFixture
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPropertyRepository> _propertyRepositoryMock;
    private readonly Mock<IOrganizationContext> _orgContextMock;
    private readonly Mock<INumberSequenceService> _numberSequenceServiceMock;
    private readonly Mock<ILogger<CreatePropertyCommandHandler>> _loggerMock;
    private readonly CreatePropertyCommandHandler _handler;

    public CreatePropertyCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _propertyRepositoryMock = new Mock<IPropertyRepository>();
        _orgContextMock = new Mock<IOrganizationContext>();
        _numberSequenceServiceMock = new Mock<INumberSequenceService>();
        _loggerMock = new Mock<ILogger<CreatePropertyCommandHandler>>();

        _unitOfWorkMock.Setup(x => x.Properties).Returns(_propertyRepositoryMock.Object);
        _orgContextMock.Setup(x => x.IsAuthenticated).Returns(true);
        _orgContextMock.Setup(x => x.OrganizationId).Returns(Guid.NewGuid());
        _numberSequenceServiceMock.Setup(x => x.GenerateNextCodeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("PROP-001");

        _handler = new CreatePropertyCommandHandler(
            _unitOfWorkMock.Object,
            _orgContextMock.Object,
            _numberSequenceServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesPropertySuccessfully()
    {
        // Arrange
        var command = new CreatePropertyCommand
        {
            Name = Fixture.Create<string>(),
            Address = Fixture.Create<string>(),
            City = Fixture.Create<string>(),
            Type = "Apartment",
            PropertyUsageType = "Complete",
            Rent = 1500m,
            Bedrooms = 2,
            Bathrooms = 1
        };

        _unitOfWorkMock
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(command.Name, result.Data!.Name);
        
        _unitOfWorkMock.Verify(
            x => x.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ReturnsFailure()
    {
        // Arrange
        _orgContextMock.Setup(x => x.IsAuthenticated).Returns(false);

        var command = new CreatePropertyCommand
        {
            Name = Fixture.Create<string>(),
            Type = "Apartment",
            Rent = 1500m
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("not authenticated", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_WithInvalidPropertyType_ReturnsFailure()
    {
        // Arrange
        var command = new CreatePropertyCommand
        {
            Name = Fixture.Create<string>(),
            Type = "InvalidType",
            Rent = 2000m
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Invalid property type", result.ErrorMessage);
    }
}
