using FluentAssertions;
using LocaGuest.Api.Controllers;
using LocaGuest.Api.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LocaGuest.Api.Tests.Controllers;

public class PropertiesV2ControllerTests : BaseTestFixture
{
    [Fact]
    public void Controller_HasCorrectAttributes()
    {
        // Assert
        var controllerType = typeof(PropertiesV2Controller);
        
        controllerType.GetCustomAttributes(typeof(ApiControllerAttribute), false)
            .Should().NotBeEmpty();
            
        controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false)
            .Should().NotBeEmpty();
    }

    [Fact]
    public void GetProperties_MethodExists()
    {
        // Arrange
        var method = typeof(PropertiesV2Controller).GetMethod("GetProperties");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }

    [Fact]
    public void GetProperty_MethodExists()
    {
        // Arrange
        var method = typeof(PropertiesV2Controller).GetMethod("GetProperty");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }

    [Fact]
    public void CreateProperty_MethodExists()
    {
        // Arrange
        var method = typeof(PropertiesV2Controller).GetMethod("CreateProperty");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }

    [Fact]
    public void UpdateProperty_MethodExists()
    {
        // Arrange
        var method = typeof(PropertiesV2Controller).GetMethod("UpdateProperty");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }

    // Note: DeleteProperty method does not exist in PropertiesV2Controller
}
