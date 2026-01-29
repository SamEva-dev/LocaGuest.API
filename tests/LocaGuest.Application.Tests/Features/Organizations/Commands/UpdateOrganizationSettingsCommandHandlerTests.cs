using AutoFixture;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Features.Organizations.Commands.UpdateOrganizationSettings;
using LocaGuest.Application.Tests.Fixtures;
using LocaGuest.Domain.Aggregates.OrganizationAggregate;
using LocaGuest.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LocaGuest.Application.Tests.Features.Organizations.Commands;

public class UpdateOrganizationSettingsCommandHandlerTests : BaseApplicationTestFixture
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IOrganizationRepository> _organizationRepositoryMock;
    private readonly Mock<IOrganizationContext> _orgContextMock;
    private readonly Mock<ILogger<UpdateOrganizationSettingsCommandHandler>> _loggerMock;
    private readonly UpdateOrganizationSettingsCommandHandler _handler;

    public UpdateOrganizationSettingsCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _organizationRepositoryMock = new Mock<IOrganizationRepository>();
        _orgContextMock = new Mock<IOrganizationContext>();
        _loggerMock = new Mock<ILogger<UpdateOrganizationSettingsCommandHandler>>();

        _unitOfWorkMock.Setup(x => x.Organizations).Returns(_organizationRepositoryMock.Object);
        _orgContextMock.Setup(x => x.IsAuthenticated).Returns(true);

        _handler = new UpdateOrganizationSettingsCommandHandler(
            _unitOfWorkMock.Object,
            _orgContextMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_UpdatesOrganizationSuccessfully()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var organization = Organization.Create(
            001,
            "Test Organization",
            "test@org.com");

        _organizationRepositoryMock
            .Setup(x => x.GetByIdAsync(organizationId, It.IsAny<CancellationToken>(), false))
            .ReturnsAsync(organization);

        _unitOfWorkMock
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateOrganizationSettingsCommand
        {
            OrganizationId = organizationId,
            Name = "Updated Organization",
            Email = "updated@org.com",
            Phone = "+1234567890",
            LogoUrl = "/uploads/logos/test.png",
            PrimaryColor = "#FF0000",
            SecondaryColor = "#00FF00",
            AccentColor = "#0000FF",
            Website = "https://test.com"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(command.Name, result.Data!.Name);
        Assert.Equal(command.Email, result.Data.Email);
        Assert.Equal(command.LogoUrl, result.Data.LogoUrl);
        Assert.Equal(command.PrimaryColor, result.Data.PrimaryColor);
        Assert.Equal(command.SecondaryColor, result.Data.SecondaryColor);
        Assert.Equal(command.AccentColor, result.Data.AccentColor);

        _unitOfWorkMock.Verify(
            x => x.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithBrandingOnly_UpdatesBrandingSuccessfully()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var organization = Organization.Create(
           001,
            "Test Organization",
            "test@org.com");

        _organizationRepositoryMock
            .Setup(x => x.GetByIdAsync(organizationId, It.IsAny<CancellationToken>(), false))
            .ReturnsAsync(organization);

        _unitOfWorkMock
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateOrganizationSettingsCommand
        {
            OrganizationId = organizationId,
            LogoUrl = "/uploads/logos/new-logo.png",
            PrimaryColor = "#3B82F6",
            SecondaryColor = "#10B981",
            AccentColor = "#F59E0B"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(command.LogoUrl, result.Data!.LogoUrl);
        Assert.Equal(command.PrimaryColor, result.Data.PrimaryColor);

        _unitOfWorkMock.Verify(
            x => x.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOrganizationNotFound_ReturnsFailure()
    {
        // Arrange
        var organizationId = Guid.NewGuid();

        _organizationRepositoryMock
            .Setup(x => x.GetByIdAsync(organizationId, It.IsAny<CancellationToken>(), false))
            .ReturnsAsync((Organization?)null);

        var command = new UpdateOrganizationSettingsCommand
        {
            OrganizationId = organizationId,
            Name = "Test"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.ErrorMessage);

        _unitOfWorkMock.Verify(
            x => x.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ReturnsFailure()
    {
        // Arrange
        _orgContextMock.Setup(x => x.IsAuthenticated).Returns(false);

        var command = new UpdateOrganizationSettingsCommand
        {
            OrganizationId = Guid.NewGuid(),
            Name = "Test"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("not authenticated", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_WhenCommitFails_ReturnsFailure()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var organization = Organization.Create(
            0001,
            "Test Organization",
            "test@org.com");

        _organizationRepositoryMock
            .Setup(x => x.GetByIdAsync(organizationId, It.IsAny<CancellationToken>(), false))
            .ReturnsAsync(organization);

        _unitOfWorkMock
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0); // Simulate save failure

        var command = new UpdateOrganizationSettingsCommand
        {
            OrganizationId = organizationId,
            Name = "Updated Organization"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Failed to save", result.ErrorMessage);
    }
}
