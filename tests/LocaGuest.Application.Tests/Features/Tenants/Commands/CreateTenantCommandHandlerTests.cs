using AutoFixture;
using FluentAssertions;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Features.Tenants.Commands.CreateTenant;
using LocaGuest.Application.Tests.Fixtures;
using LocaGuest.Domain.Aggregates.TenantAggregate;
using LocaGuest.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LocaGuest.Application.Tests.Features.Tenants.Commands;

public class CreateTenantCommandHandlerTests : BaseApplicationTestFixture
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ITenantRepository> _tenantRepositoryMock;
    private readonly Mock<ITenantContext> _tenantContextMock;
    private readonly Mock<ILogger<CreateTenantCommandHandler>> _loggerMock;
    private readonly CreateTenantCommandHandler _handler;

    public CreateTenantCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _tenantRepositoryMock = new Mock<ITenantRepository>();
        _tenantContextMock = new Mock<ITenantContext>();
        _loggerMock = new Mock<ILogger<CreateTenantCommandHandler>>();

        _unitOfWorkMock.Setup(x => x.Tenants).Returns(_tenantRepositoryMock.Object);
        _tenantContextMock.Setup(x => x.IsAuthenticated).Returns(true);

        _handler = new CreateTenantCommandHandler(
            _unitOfWorkMock.Object,
            _tenantContextMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesTenantSuccessfully()
    {
        // Arrange
        var command = new CreateTenantCommand
        {
            FirstName = Fixture.Create<string>(),
            LastName = Fixture.Create<string>(),
            Email = $"{Fixture.Create<string>()}@test.com",
            Phone = Fixture.Create<string>()
        };

        _unitOfWorkMock
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Email.Should().Be(command.Email);
        
        _unitOfWorkMock.Verify(
            x => x.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ReturnsFailure()
    {
        // Arrange
        _tenantContextMock.Setup(x => x.IsAuthenticated).Returns(false);

        var command = new CreateTenantCommand
        {
            FirstName = Fixture.Create<string>(),
            LastName = Fixture.Create<string>(),
            Email = "test@test.com"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("not authenticated");
    }
}
