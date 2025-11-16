using FluentAssertions;
using LocaGuest.Api.Controllers;
using LocaGuest.Api.Tests.Builders;
using LocaGuest.Api.Tests.Fixtures;
using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Models;
using LocaGuest.Application.DTOs.Tenants;
using LocaGuest.Application.Features.Tenants.Commands.CreateTenant;
using LocaGuest.Application.Features.Tenants.Queries.GetTenants;
using LocaGuest.Application.Features.Tenants.Queries.GetTenant;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LocaGuest.Api.Tests.Controllers;

public class TenantsControllerTests : BaseTestFixture
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<TenantsController>> _loggerMock;
    private readonly TenantsController _controller;

    public TenantsControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<TenantsController>>();
        _controller = new TenantsController(_mediatorMock.Object, _loggerMock.Object);
    }

    #region GetTenants Tests

    [Fact]
    public async Task GetTenants_WithSuccessfulQuery_ReturnsOkWithTenants()
    {
        // Arrange
        var tenants = new List<TenantDto>
        {
            TenantDtoBuilder.ATenant().WithFullName("John Doe").Build(),
            TenantDtoBuilder.ATenant().WithFullName("Jane Smith").Build()
        };

        var pagedResult = new PagedResult<TenantDto>
        {
            Items = tenants,
            TotalCount = 2,
            Page = 1,
            PageSize = 10
        };

        var result = Result.Success(pagedResult);
        var query = new GetTenantsQuery();

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetTenantsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var actionResult = await _controller.GetTenants(query);

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(pagedResult);

        _mediatorMock.Verify(m => m.Send(
            It.IsAny<GetTenantsQuery>(),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task GetTenants_WithFailedQuery_ReturnsBadRequest()
    {
        // Arrange
        var errorMessage = "Failed to retrieve tenants";
        var result = Result.Failure<PagedResult<TenantDto>>(errorMessage);
        var query = new GetTenantsQuery();

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetTenantsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var actionResult = await _controller.GetTenants(query);

        // Assert
        actionResult.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = actionResult as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo(new { message = errorMessage });
    }

    [Fact]
    public async Task GetTenants_WithPagination_SendsCorrectQuery()
    {
        // Arrange
        var query = new GetTenantsQuery
        {
            Page = 2,
            PageSize = 20,
            Search = "john"
        };

        var result = Result.Success(new PagedResult<TenantDto> 
        { 
            Items = new List<TenantDto>(),
            TotalCount = 0,
            Page = 2,
            PageSize = 20
        });

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetTenantsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        await _controller.GetTenants(query);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<GetTenantsQuery>(q => 
                q.Page == 2 && 
                q.PageSize == 20 && 
                q.Search == "john"),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Theory]
    [InlineData(1, 10, "")]
    [InlineData(1, 25, "test")]
    [InlineData(3, 50, null)]
    public async Task GetTenants_WithVariousPaginationParameters_SendsCorrectQuery(
        int page, int pageSize, string? search)
    {
        // Arrange
        var query = new GetTenantsQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search
        };

        var result = Result.Success(new PagedResult<TenantDto> 
        { 
            Items = new List<TenantDto>(),
            TotalCount = 0,
            Page = page,
            PageSize = pageSize
        });

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetTenantsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        await _controller.GetTenants(query);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<GetTenantsQuery>(q => 
                q.Page == page && 
                q.PageSize == pageSize),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    #endregion

    #region GetTenant Tests

    [Fact]
    public async Task GetTenant_WithValidId_ReturnsOkWithTenant()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = TenantDtoBuilder.ATenant()
            .WithId(tenantId)
            .WithFullName("John Doe")
            .Build();

        var result = Result.Success(tenant);

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetTenantQuery>(q => q.Id == tenantId.ToString()), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var actionResult = await _controller.GetTenant(tenantId.ToString());

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(tenant);

        _mediatorMock.Verify(m => m.Send(
            It.Is<GetTenantQuery>(q => q.Id == tenantId.ToString()),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task GetTenant_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var errorMessage = "Tenant not found";
        var result = Result.Failure<TenantDto>(errorMessage);

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetTenantQuery>(q => q.Id == tenantId.ToString()), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var actionResult = await _controller.GetTenant(tenantId.ToString());

        // Assert
        actionResult.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = actionResult as NotFoundObjectResult;
        notFoundResult!.Value.Should().BeEquivalentTo(new { message = errorMessage });
    }

    [Fact]
    public async Task GetTenant_WithDatabaseError_ReturnsBadRequest()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var errorMessage = "Database connection failed";
        var result = Result.Failure<TenantDto>(errorMessage);

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetTenantQuery>(q => q.Id == tenantId.ToString()), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var actionResult = await _controller.GetTenant(tenantId.ToString());

        // Assert
        actionResult.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region CreateTenant Tests

    [Fact]
    public async Task CreateTenant_WithValidCommand_ReturnsCreatedWithTenant()
    {
        // Arrange
        var command = new CreateTenantCommand
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@test.com",
            Phone = "+33612345678",
            DateOfBirth = new DateTime(1990, 1, 1)
        };

        var createdTenant = TenantDetailDtoBuilder.ATenant()
            .WithFullName($"{command.FirstName} {command.LastName}")
            .WithEmail(command.Email)
            .WithPhone(command.Phone)
            .Build();

        var result = Result.Success(createdTenant);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateTenantCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var actionResult = await _controller.CreateTenant(command);

        // Assert
        actionResult.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = actionResult as CreatedAtActionResult;
        createdResult!.Value.Should().BeEquivalentTo(createdTenant);

        _mediatorMock.Verify(m => m.Send(command, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTenant_WithInvalidCommand_ReturnsBadRequest()
    {
        // Arrange
        var command = new CreateTenantCommand
        {
            FirstName = "John"
            // Missing required fields
        };

        var errorMessage = "Validation failed";
        var result = Result.Failure<TenantDetailDto>(errorMessage);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateTenantCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var actionResult = await _controller.CreateTenant(command);

        // Assert
        actionResult.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = actionResult as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo(new { message = errorMessage });
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task CreateTenant_WithInvalidEmail_ReturnsBadRequest(string? invalidEmail)
    {
        // Arrange
        var command = new CreateTenantCommand
        {
            FirstName = "John",
            LastName = "Doe",
            Email = invalidEmail!
        };

        var result = Result.Failure<TenantDetailDto>("Email is required");

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateTenantCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var actionResult = await _controller.CreateTenant(command);

        // Assert
        actionResult.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateTenant_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var command = new CreateTenantCommand
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@test.com",
            Phone = "+33612345678"
        };

        var errorMessage = "Email already exists";
        var result = Result.Failure<TenantDetailDto>(errorMessage);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateTenantCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var actionResult = await _controller.CreateTenant(command);

        // Assert
        actionResult.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = actionResult as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo(new { message = errorMessage });
    }

    #endregion
}
