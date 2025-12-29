using AutoFixture;
using FluentAssertions;
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
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly Mock<ILogger<UpdateOrganizationSettingsCommandHandler>> _loggerMock;
    private readonly UpdateOrganizationSettingsCommandHandler _handler;

    public UpdateOrganizationSettingsCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _organizationRepositoryMock = new Mock<IOrganizationRepository>();
        _tenantContextMock = new Mock<ITenantContext>();
        _loggerMock = new Mock<ILogger<UpdateOrganizationSettingsCommandHandler>>();

        _unitOfWorkMock.Setup(x => x.Organizations).Returns(_organizationRepositoryMock.Object);
        _tenantContextMock.Setup(x => x.IsAuthenticated).Returns(true);

        _handler = new UpdateOrganizationSettingsCommandHandler(
            _unitOfWorkMock.Object,
            _tenantContextMock.Object,
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
            .Setup(x => x.GetByIdAsync(organizationId, It.IsAny<CancellationToken>()))
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
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be(command.Name);
        result.Data.Email.Should().Be(command.Email);
        result.Data.LogoUrl.Should().Be(command.LogoUrl);
        result.Data.PrimaryColor.Should().Be(command.PrimaryColor);
        result.Data.SecondaryColor.Should().Be(command.SecondaryColor);
        result.Data.AccentColor.Should().Be(command.AccentColor);

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
            .Setup(x => x.GetByIdAsync(organizationId, It.IsAny<CancellationToken>()))
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
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.LogoUrl.Should().Be(command.LogoUrl);
        result.Data.PrimaryColor.Should().Be(command.PrimaryColor);

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
            .Setup(x => x.GetByIdAsync(organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Organization?)null);

        var command = new UpdateOrganizationSettingsCommand
        {
            OrganizationId = organizationId,
            Name = "Test"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("not found");

        _unitOfWorkMock.Verify(
            x => x.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ReturnsFailure()
    {
        // Arrange
        _tenantContextMock.Setup(x => x.IsAuthenticated).Returns(false);

        var command = new UpdateOrganizationSettingsCommand
        {
            OrganizationId = Guid.NewGuid(),
            Name = "Test"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("not authenticated");
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
            .Setup(x => x.GetByIdAsync(organizationId, It.IsAny<CancellationToken>()))
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
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Failed to save");
    }
}
