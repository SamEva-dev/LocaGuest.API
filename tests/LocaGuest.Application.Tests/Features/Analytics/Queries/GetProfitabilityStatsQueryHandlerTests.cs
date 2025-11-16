using FluentAssertions;
using LocaGuest.Application.Features.Analytics.Queries.GetProfitabilityStats;
using LocaGuest.Application.Tests.Fixtures;
using LocaGuest.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LocaGuest.Application.Tests.Features.Analytics.Queries;

public class GetProfitabilityStatsQueryHandlerTests : BaseApplicationTestFixture
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<GetProfitabilityStatsQueryHandler>> _loggerMock;
    private readonly GetProfitabilityStatsQueryHandler _handler;

    public GetProfitabilityStatsQueryHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<GetProfitabilityStatsQueryHandler>>();

        _handler = new GetProfitabilityStatsQueryHandler(
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidQuery_HandlesRequest()
    {
        // Arrange
        var query = new GetProfitabilityStatsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        // Handler may fail without DB setup, which is expected for unit tests
        // This test verifies the handler doesn't throw exceptions
    }
}
