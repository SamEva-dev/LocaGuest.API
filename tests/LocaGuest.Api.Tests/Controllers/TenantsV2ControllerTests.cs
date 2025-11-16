using FluentAssertions;
using LocaGuest.Api.Controllers;
using LocaGuest.Api.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LocaGuest.Api.Tests.Controllers;

public class TenantsV2ControllerTests : BaseTestFixture
{
    [Fact]
    public void Controller_HasCorrectAttributes()
    {
        // Assert
        var controllerType = typeof(TenantsV2Controller);
        
        controllerType.GetCustomAttributes(typeof(ApiControllerAttribute), false)
            .Should().NotBeEmpty();
            
        controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false)
            .Should().NotBeEmpty();
    }

    [Fact]
    public void GetTenants_MethodExists()
    {
        // Arrange
        var method = typeof(TenantsV2Controller).GetMethod("GetTenants");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }

    [Fact]
    public void GetTenant_MethodExists()
    {
        // Arrange
        var method = typeof(TenantsV2Controller).GetMethod("GetTenant");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }

    [Fact]
    public void CreateTenant_MethodExists()
    {
        // Arrange
        var method = typeof(TenantsV2Controller).GetMethod("CreateTenant");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }

    [Fact]
    public void UpdateTenant_MethodExists()
    {
        // Arrange
        var method = typeof(TenantsV2Controller).GetMethod("UpdateTenant");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }

    // Note: DeleteTenant method does not exist in TenantsV2Controller
}
