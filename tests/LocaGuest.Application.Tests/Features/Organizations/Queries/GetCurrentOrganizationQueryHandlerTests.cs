using AutoFixture;
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
    private readonly Mock<IOrganizationContext> _orgContextMock;
    private readonly Mock<ILogger<GetCurrentOrganizationQueryHandler>> _loggerMock;
    private readonly GetCurrentOrganizationQueryHandler _handler;

    public GetCurrentOrganizationQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _organizationRepositoryMock = new Mock<IOrganizationRepository>();
        _orgContextMock = new Mock<IOrganizationContext>();
        _loggerMock = new Mock<ILogger<GetCurrentOrganizationQueryHandler>>();

        _unitOfWorkMock.Setup(x => x.Organizations).Returns(_organizationRepositoryMock.Object);
        _orgContextMock.Setup(x => x.IsAuthenticated).Returns(true);

        _handler = new GetCurrentOrganizationQueryHandler(
            _unitOfWorkMock.Object,
            _orgContextMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingOrganization_ReturnsOrganizationSuccessfully()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        _orgContextMock.Setup(x => x.OrganizationId).Returns(organizationId);

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
            .Setup(x => x.GetByIdAsync(organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);

        var query = new GetCurrentOrganizationQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("T0001", result.Data!.Code);
        Assert.Equal("Test Organization", result.Data.Name);
        Assert.Equal("test@org.com", result.Data.Email);
        Assert.Equal("+1234567890", result.Data.Phone);
        Assert.Equal("/uploads/logos/test.png", result.Data.LogoUrl);
        Assert.Equal("#FF0000", result.Data.PrimaryColor);
        Assert.Equal("#00FF00", result.Data.SecondaryColor);
        Assert.Equal("#0000FF", result.Data.AccentColor);
        Assert.Equal("https://test.com", result.Data.Website);
    }

    [Fact]
    public async Task Handle_WithMultipleOrganizations_ReturnsFirstOrganization()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        _orgContextMock.Setup(x => x.OrganizationId).Returns(organizationId);

        var organization1 = Organization.Create(0001, "Organization 1", "org1@test.com");
        var organization2 = Organization.Create(0002, "Organization 2", "org2@test.com");

        _organizationRepositoryMock
            .Setup(x => x.GetByIdAsync(organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization1);

        var query = new GetCurrentOrganizationQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("T0001", result.Data!.Code);
    }

    [Fact]
    public async Task Handle_WithNoBranding_ReturnsOrganizationWithNullBrandingFields()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        _orgContextMock.Setup(x => x.OrganizationId).Returns(organizationId);

        var organization = Organization.Create(
            001,
            "Test Organization",
            "test@org.com");
        // Don't set branding

        _organizationRepositoryMock
            .Setup(x => x.GetByIdAsync(organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);

        var query = new GetCurrentOrganizationQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Null(result.Data!.LogoUrl);
        Assert.Null(result.Data.PrimaryColor);
        Assert.Null(result.Data.SecondaryColor);
        Assert.Null(result.Data.AccentColor);
    }

    [Fact]
    public async Task Handle_WhenNoOrganizationExists_ReturnsFailure()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        _orgContextMock.Setup(x => x.OrganizationId).Returns(organizationId);

        _organizationRepositoryMock
            .Setup(x => x.GetByIdAsync(organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Organization?)null);

        var query = new GetCurrentOrganizationQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("No organization found", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ReturnsFailure()
    {
        // Arrange
        _orgContextMock.Setup(x => x.IsAuthenticated).Returns(false);

        var query = new GetCurrentOrganizationQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("not authenticated", result.ErrorMessage);
    }
}
