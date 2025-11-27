using AutoFixture;
using FluentAssertions;
using LocaGuest.Api.Controllers;
using LocaGuest.Api.Tests.Builders;
using LocaGuest.Api.Tests.Fixtures;
using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Common.Models;
using LocaGuest.Application.DTOs.Properties;
using LocaGuest.Application.Features.Properties.Commands.CreateProperty;
using LocaGuest.Application.Features.Properties.Queries.GetProperties;
using LocaGuest.Application.Features.Properties.Queries.GetProperty;
using LocaGuest.Application.Services;
using LocaGuest.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LocaGuest.Api.Tests.Controllers;

public class PropertiesControllerTests : BaseTestFixture
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<PropertiesController>> _loggerMock;
    private readonly LocaGuestDbContext _dbContext;
    private readonly PropertiesController _controller;

    public PropertiesControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<PropertiesController>>();
        
        // Create in-memory database for tests
        var options = new DbContextOptionsBuilder<LocaGuestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var mediatorForDbMock = new Mock<IMediator>();
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        var tenantContextMock = new Mock<ITenantContext>();
        
        _dbContext = new LocaGuestDbContext(options, mediatorForDbMock.Object, currentUserServiceMock.Object, tenantContextMock.Object);
        
        _controller = new PropertiesController(_mediatorMock.Object, _loggerMock.Object, _dbContext);
    }

    #region GetProperties Tests

    [Fact]
    public async Task GetProperties_WithSuccessfulQuery_ReturnsOkWithProperties()
    {
        // Arrange - Using AutoFixture + Builders for test data generation
        var properties = new List<PropertyDto>
        {
            PropertyDtoBuilder.AProperty().WithName(Fixture.Create<string>()).Build(),
            PropertyDtoBuilder.AProperty().WithName(Fixture.Create<string>()).Build()
        };

        var pagedResult = new PagedResult<PropertyDto>
        {
            Items = properties,
            TotalCount = properties.Count,
            Page = 1,
            PageSize = 10
        };

        var result = Result.Success(pagedResult);
        var query = new GetPropertiesQuery();

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetPropertiesQuery>(q => true), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var actionResult = await _controller.GetProperties(query);

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(pagedResult);

        _mediatorMock.Verify(m => m.Send(
            It.IsAny<GetPropertiesQuery>(),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task GetProperties_WithFailedQuery_ReturnsBadRequest()
    {
        // Arrange
        var errorMessage = "Failed to retrieve properties";
        var result = Result.Failure<PagedResult<PropertyDto>>(errorMessage);
        var query = new GetPropertiesQuery();

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPropertiesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var actionResult = await _controller.GetProperties(query);

        // Assert
        actionResult.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = actionResult as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo(new { message = errorMessage });
    }

    [Fact]
    public async Task GetProperties_WithPagination_SendsCorrectQuery()
    {
        // Arrange - AutoFixture generates non-critical data
        var query = new GetPropertiesQuery
        {
            Page = 2,
            PageSize = 20,
            Search = "apartment"
        };

        var pagedResult = new PagedResult<PropertyDto> 
        { 
            Items = Fixture.CreateMany<PropertyDto>(0).ToList(),
            TotalCount = 0,
            Page = 2,
            PageSize = 20
        };

        var result = Result.Success(pagedResult);

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetPropertiesQuery>(q => true), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        await _controller.GetProperties(query);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<GetPropertiesQuery>(q => 
                q.Page == 2 && 
                q.PageSize == 20 && 
                q.Search == "apartment"),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Theory]
    [InlineData(1, 10, "")]
    [InlineData(1, 25, "test")]
    [InlineData(3, 50, null)]
    public async Task GetProperties_WithVariousPaginationParameters_SendsCorrectQuery(
        int page, int pageSize, string? search)
    {
        // Arrange - Using Theory parameters with AutoFixture for collections
        var query = new GetPropertiesQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search
        };

        var pagedResult = new PagedResult<PropertyDto> 
        { 
            Items = Fixture.CreateMany<PropertyDto>(0).ToList(),
            TotalCount = 0,
            Page = page,
            PageSize = pageSize
        };

        var result = Result.Success(pagedResult);

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetPropertiesQuery>(q => true), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        await _controller.GetProperties(query);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<GetPropertiesQuery>(q => 
                q.Page == page && 
                q.PageSize == pageSize),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    #endregion

    #region GetProperty Tests

    [Fact]
    public async Task GetProperty_WithValidId_ReturnsOkWithProperty()
    {
        // Arrange - Combining AutoFixture with Builder
        var propertyId = Fixture.Create<Guid>();
        var propertyName = Fixture.Create<string>();
        
        var property = PropertyDtoBuilder.AProperty()
            .WithId(propertyId)
            .WithName(propertyName)
            .Build();

        var result = Result.Success(property);

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetPropertyQuery>(q => q.Id == propertyId.ToString()), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var actionResult = await _controller.GetProperty(propertyId.ToString());

        // Assert
        actionResult.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(property);

        _mediatorMock.Verify(m => m.Send(
            It.Is<GetPropertyQuery>(q => q.Id == propertyId.ToString()),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task GetProperty_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var propertyId = Guid.NewGuid();
        var errorMessage = "Property not found";
        var result = Result.Failure<PropertyDto>(errorMessage);

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetPropertyQuery>(q => q.Id == propertyId.ToString()), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var actionResult = await _controller.GetProperty(propertyId.ToString());

        // Assert
        actionResult.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = actionResult as NotFoundObjectResult;
        notFoundResult!.Value.Should().BeEquivalentTo(new { message = errorMessage });
    }

    [Fact]
    public async Task GetProperty_WithDatabaseError_ReturnsBadRequest()
    {
        // Arrange
        var propertyId = Guid.NewGuid();
        var errorMessage = "Database connection failed";
        var result = Result.Failure<PropertyDto>(errorMessage);

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetPropertyQuery>(q => q.Id == propertyId.ToString()), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var actionResult = await _controller.GetProperty(propertyId.ToString());

        // Assert
        actionResult.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = actionResult as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo(new { message = errorMessage });
    }

    #endregion

    #region CreateProperty Tests

    // TODO: Fix Moq generic type inference issue with PropertyDetailDto
    // [Fact]
    // public async Task CreateProperty_WithValidCommand_ReturnsCreatedWithProperty()
    // {
    //     // Test temporairement désactivé - problème de typage Moq
    // }

    [Fact]
    public async Task CreateProperty_WithInvalidCommand_ReturnsBadRequest()
    {
        // Arrange - Using AutoFixture for error messages
        var command = new CreatePropertyCommand
        {
            Name = string.Empty // Invalid: empty name
        };

        var errorMessage = Fixture.Create<string>();
        var result = Result.Failure<PropertyDetailDto>(errorMessage);

        _mediatorMock
            .Setup(m => m.Send(It.Is<CreatePropertyCommand>(c => true), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var actionResult = await _controller.CreateProperty(command);

        // Assert
        actionResult.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = actionResult as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo(new { message = errorMessage });
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task CreateProperty_WithInvalidName_ReturnsBadRequest(string? invalidName)
    {
        // Arrange
        var command = new CreatePropertyCommand
        {
            Name = invalidName!
        };

        var result = Result.Failure<PropertyDetailDto>("Name is required");

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreatePropertyCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var actionResult = await _controller.CreateProperty(command);

        // Assert
        actionResult.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateProperty_WithNegativeRent_ReturnsBadRequest()
    {
        // Arrange - Using AutoFixture for random property name
        var command = new CreatePropertyCommand
        {
            Name = Fixture.Create<string>(),
            Rent = -1000m // Invalid: negative rent
        };

        var errorMessage = "Rent must be positive";
        var result = Result.Failure<PropertyDetailDto>(errorMessage);

        _mediatorMock
            .Setup(m => m.Send(It.Is<CreatePropertyCommand>(c => true), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var actionResult = await _controller.CreateProperty(command);

        // Assert
        actionResult.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = actionResult as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo(new { message = errorMessage });
    }

    #endregion
}
