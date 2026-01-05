using AutoFixture;
using FluentAssertions;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Features.Properties.Queries.GetProperties;
using LocaGuest.Application.Tests.Fixtures;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LocaGuest.Application.Tests.Features.Properties.Queries;

public class GetPropertiesQueryHandlerTests : BaseApplicationTestFixture
{
    private readonly LocaGuestDbContext _db;
    private readonly ILocaGuestReadDbContext _readDb;
    private readonly Mock<ILogger<GetPropertiesQueryHandler>> _loggerMock;
    private readonly GetPropertiesQueryHandler _handler;
    private static readonly Guid TestOrgId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public GetPropertiesQueryHandlerTests()
    {
        _loggerMock = new Mock<ILogger<GetPropertiesQueryHandler>>();

        var options = new DbContextOptionsBuilder<LocaGuestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var mediator = new Mock<MediatR.IMediator>();

        var orgContext = new Mock<IOrganizationContext>();
        orgContext.Setup(x => x.IsAuthenticated).Returns(true);
        orgContext.Setup(x => x.OrganizationId).Returns(TestOrgId);
        orgContext.Setup(x => x.IsSystemContext).Returns(false);
        orgContext.Setup(x => x.CanBypassOrganizationFilter).Returns(false);

        _db = new LocaGuestDbContext(options, mediator.Object, organizationContext: orgContext.Object);
        _readDb = _db;

        _handler = new GetPropertiesQueryHandler(_readDb, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidQuery_ReturnsPagedProperties()
    {
        // Arrange
        var query = new GetPropertiesQuery { Page = 1, PageSize = 10 };

        var p = Property.Create(
            name: "Test",
            address: "1 rue test",
            city: "Paris",
            type: PropertyType.Apartment,
            usageType: PropertyUsageType.Complete,
            rent: 100m,
            bedrooms: 1,
            bathrooms: 1);

        p.SetOrganizationId(TestOrgId);
        _db.Properties.Add(p);

        await _db.SaveChangesAsync();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_QueryIsCalledOnRepository()
    {
        // Arrange
        var query = new GetPropertiesQuery { Page = 1, PageSize = 10 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }
}
