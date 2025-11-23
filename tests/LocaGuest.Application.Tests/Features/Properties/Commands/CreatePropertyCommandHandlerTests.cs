using AutoFixture;
using FluentAssertions;
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
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly Mock<INumberSequenceService> _numberSequenceServiceMock;
    private readonly Mock<ILogger<CreatePropertyCommandHandler>> _loggerMock;
    private readonly CreatePropertyCommandHandler _handler;

    public CreatePropertyCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _propertyRepositoryMock = new Mock<IPropertyRepository>();
        _tenantContextMock = new Mock<ITenantContext>();
        _numberSequenceServiceMock = new Mock<INumberSequenceService>();
        _loggerMock = new Mock<ILogger<CreatePropertyCommandHandler>>();

        _unitOfWorkMock.Setup(x => x.Properties).Returns(_propertyRepositoryMock.Object);
        _tenantContextMock.Setup(x => x.IsAuthenticated).Returns(true);
        _numberSequenceServiceMock.Setup(x => x.GenerateNextCodeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("PROP-001");

        _handler = new CreatePropertyCommandHandler(
            _unitOfWorkMock.Object,
            _tenantContextMock.Object,
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
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be(command.Name);
        
        _unitOfWorkMock.Verify(
            x => x.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ReturnsFailure()
    {
        // Arrange
        _tenantContextMock.Setup(x => x.IsAuthenticated).Returns(false);

        var command = new CreatePropertyCommand
        {
            Name = Fixture.Create<string>(),
            Type = "Apartment",
            Rent = 1500m
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("not authenticated");
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
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Invalid property type");
    }
}
