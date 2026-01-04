using FluentAssertions;
using LocaGuest.Api.Controllers;
using LocaGuest.Api.Tests.Fixtures;
using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Features.Organizations.Commands.CreateOrganization;
using LocaGuest.Application.Features.Organizations.Commands.UpdateOrganizationSettings;
using UpdateSettingsDto = LocaGuest.Application.Features.Organizations.Commands.UpdateOrganizationSettings.OrganizationDto;
using LocaGuest.Application.Features.Organizations.Queries.GetAllOrganizations;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace LocaGuest.Api.Tests.Controllers;

public class OrganizationsControllerTests : BaseTestFixture
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<OrganizationsController>> _loggerMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly IConfiguration _configuration;
    private readonly OrganizationsController _controller;

    public OrganizationsControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<OrganizationsController>>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:FrontendUrl"] = "http://localhost:4200"
            })
            .Build();

        _controller = new OrganizationsController(
            _mediatorMock.Object, 
            _loggerMock.Object, 
            _fileStorageServiceMock.Object,
            _configuration);

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "SuperAdmin")
        }, authenticationType: "Test");

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
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

    #region UploadLogo Tests

    [Fact]
    public async Task UploadLogo_WithValidFile_ReturnsOkWithLogoUrl()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var fileMock = new Mock<IFormFile>();
        var content = "Test file content"u8.ToArray();
        var ms = new MemoryStream(content);
        
        fileMock.Setup(f => f.FileName).Returns("test.png");
        fileMock.Setup(f => f.ContentType).Returns("image/png");
        fileMock.Setup(f => f.Length).Returns(content.Length);
        fileMock.Setup(f => f.OpenReadStream()).Returns(ms);

        _fileStorageServiceMock
            .Setup(x => x.ValidateFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()))
            .Returns(true);

        _fileStorageServiceMock
            .Setup(x => x.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("uploads/logos/test-guid.png");

        var mockDto = new UpdateSettingsDto
        {
            Id = organizationId,
            LogoUrl = "/uploads/logos/test-guid.png"
        };

        _mediatorMock
            .Setup(x => x.Send(It.Is<UpdateOrganizationSettingsCommand>(c => c.OrganizationId == organizationId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UpdateSettingsDto>.Success(mockDto));

        // Act
        var result = await _controller.UploadLogo(organizationId, fileMock.Object);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        
        _fileStorageServiceMock.Verify(
            x => x.ValidateFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()),
            Times.Once);
        
        _fileStorageServiceMock.Verify(
            x => x.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), "logos", It.IsAny<CancellationToken>()),
            Times.Once);

        _mediatorMock.Verify(
            x => x.Send(It.IsAny<UpdateOrganizationSettingsCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UploadLogo_WithNullFile_ReturnsBadRequest()
    {
        // Arrange
        var organizationId = Guid.NewGuid();

        // Act
        var result = await _controller.UploadLogo(organizationId, null!);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        
        _fileStorageServiceMock.Verify(
            x => x.ValidateFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()),
            Times.Never);
    }

    [Fact]
    public async Task UploadLogo_WithInvalidFile_ReturnsBadRequest()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var fileMock = new Mock<IFormFile>();
        
        fileMock.Setup(f => f.FileName).Returns("test.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.Length).Returns(1024);

        _fileStorageServiceMock
            .Setup(x => x.ValidateFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()))
            .Returns(false);

        // Act
        var result = await _controller.UploadLogo(organizationId, fileMock.Object);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        
        _fileStorageServiceMock.Verify(
            x => x.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UploadLogo_WhenUpdateFails_DeletesFileAndReturnsBadRequest()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var fileMock = new Mock<IFormFile>();
        var content = "Test file content"u8.ToArray();
        var ms = new MemoryStream(content);
        
        fileMock.Setup(f => f.FileName).Returns("test.png");
        fileMock.Setup(f => f.ContentType).Returns("image/png");
        fileMock.Setup(f => f.Length).Returns(content.Length);
        fileMock.Setup(f => f.OpenReadStream()).Returns(ms);

        _fileStorageServiceMock
            .Setup(x => x.ValidateFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()))
            .Returns(true);

        _fileStorageServiceMock
            .Setup(x => x.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("uploads/logos/test-guid.png");

        _mediatorMock
            .Setup(x => x.Send(It.Is<UpdateOrganizationSettingsCommand>(c => c.OrganizationId == organizationId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UpdateSettingsDto>.Failure<UpdateSettingsDto>("Organization not found"));

        // Act
        var result = await _controller.UploadLogo(organizationId, fileMock.Object);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        
        _fileStorageServiceMock.Verify(
            x => x.DeleteFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
