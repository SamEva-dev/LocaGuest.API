using FluentAssertions;
using LocaGuest.Api.Controllers;
using LocaGuest.Api.Tests.Fixtures;
using LocaGuest.Application.Common;
using LocaGuest.Application.Features.Organizations.Commands.CreateOrganization;
using LocaGuest.Application.Features.Organizations.Queries.GetAllOrganizations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LocaGuest.Api.Tests.Controllers;

public class OrganizationsControllerTests : BaseTestFixture
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<OrganizationsController>> _loggerMock;
    private readonly OrganizationsController _controller;

    public OrganizationsControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<OrganizationsController>>();
        _controller = new OrganizationsController(_mediatorMock.Object, _loggerMock.Object);
    }

    #region GetAllOrganizations Tests

    [Fact]
    public async Task GetAllOrganizations_WhenCalled_CallsMediatorSend()
    {
        // Arrange & Act
        try
        {
            await _controller.GetAllOrganizations();
        }
        catch
        {
            // Ignore - we just want to verify Send was called
        }

        // Assert
        _mediatorMock.Verify(m => m.Send(It.IsAny<GetAllOrganizationsQuery>(), default), Times.Once);
    }

    #endregion

    #region CreateOrganization Tests

    [Fact]
    public async Task CreateOrganization_WithValidCommand_CallsMediatorSend()
    {
        // Arrange
        var command = new CreateOrganizationCommand
        {
            Name = "Test Org",
            Email = "test@test.com"
        };

        // Act
        try
        {
            await _controller.CreateOrganization(command);
        }
        catch
        {
            // Ignore - we just want to verify Send was called
        }

        // Assert
        _mediatorMock.Verify(m => m.Send(It.IsAny<CreateOrganizationCommand>(), default), Times.Once);
    }

    #endregion
}
