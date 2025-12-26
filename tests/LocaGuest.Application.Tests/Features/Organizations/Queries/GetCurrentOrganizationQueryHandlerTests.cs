using AutoFixture;
using FluentAssertions;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Features.Organizations.Queries.GetCurrentOrganization;
using LocaGuest.Application.Tests.Fixtures;
using LocaGuest.Domain.Aggregates.OrganizationAggregate;
using LocaGuest.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LocaGuest.Application.Tests.Features.Organizations.Queries;

public class GetCurrentOrganizationQueryHandlerTests : BaseApplicationTestFixture
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IOrganizationRepository> _organizationRepositoryMock;
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly Mock<ILogger<GetCurrentOrganizationQueryHandler>> _loggerMock;
    private readonly GetCurrentOrganizationQueryHandler _handler;

    public GetCurrentOrganizationQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _organizationRepositoryMock = new Mock<IOrganizationRepository>();
        _tenantContextMock = new Mock<ITenantContext>();
        _loggerMock = new Mock<ILogger<GetCurrentOrganizationQueryHandler>>();

        _unitOfWorkMock.Setup(x => x.Organizations).Returns(_organizationRepositoryMock.Object);
        _tenantContextMock.Setup(x => x.IsAuthenticated).Returns(true);

        _handler = new GetCurrentOrganizationQueryHandler(
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingOrganization_ReturnsOrganizationSuccessfully()
    {
        // Arrange
        var organization = Organization.Create(
            001,
            "Test Organization",
            "test@org.com",
            "+1234567890");

        // Set branding
        organization.UpdateBrandingSettings(
            "/uploads/logos/test.png",
            "#FF0000",
            "#00FF00",
            "#0000FF",
            "https://test.com");

        _organizationRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Organization> { organization });

        var query = new GetCurrentOrganizationQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Code.Should().Be("ORG-001");
        result.Data.Name.Should().Be("Test Organization");
        result.Data.Email.Should().Be("test@org.com");
        result.Data.Phone.Should().Be("+1234567890");
        result.Data.LogoUrl.Should().Be("/uploads/logos/test.png");
        result.Data.PrimaryColor.Should().Be("#FF0000");
        result.Data.SecondaryColor.Should().Be("#00FF00");
        result.Data.AccentColor.Should().Be("#0000FF");
        result.Data.Website.Should().Be("https://test.com");
    }

    [Fact]
    public async Task Handle_WithMultipleOrganizations_ReturnsFirstOrganization()
    {
        // Arrange
        var organization1 = Organization.Create(0001, "Organization 1", "org1@test.com");
        var organization2 = Organization.Create(0002, "Organization 2", "org2@test.com");

        _organizationRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Organization> { organization1, organization2 });

        var query = new GetCurrentOrganizationQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Code.Should().Be("ORG-001");
    }

    [Fact]
    public async Task Handle_WithNoBranding_ReturnsOrganizationWithNullBrandingFields()
    {
        // Arrange
        var organization = Organization.Create(
            001,
            "Test Organization",
            "test@org.com");
        // Don't set branding

        _organizationRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Organization> { organization });

        var query = new GetCurrentOrganizationQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.LogoUrl.Should().BeNull();
        result.Data.PrimaryColor.Should().BeNull();
        result.Data.SecondaryColor.Should().BeNull();
        result.Data.AccentColor.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenNoOrganizationExists_ReturnsFailure()
    {
        // Arrange
        _organizationRepositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Organization>());

        var query = new GetCurrentOrganizationQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("No organization found");
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ReturnsFailure()
    {
        // Arrange
        _tenantContextMock.Setup(x => x.IsAuthenticated).Returns(false);

        var query = new GetCurrentOrganizationQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("not authenticated");
    }
}
