using FluentAssertions;
using LocaGuest.Api.Controllers;
using LocaGuest.Api.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LocaGuest.Api.Tests.Controllers;

public class ContractsControllerTests : BaseTestFixture
{
    [Fact]
    public void Controller_HasCorrectAttributes()
    {
        // Assert
        var controllerType = typeof(ContractsController);
        
        controllerType.GetCustomAttributes(typeof(ApiControllerAttribute), false)
            .Should().NotBeEmpty();
            
        controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false)
            .Should().NotBeEmpty();
    }

    [Fact]
    public void GetContractStats_MethodExists()
    {
        // Arrange
        var method = typeof(ContractsController).GetMethod("GetContractStats");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }

    [Fact]
    public void GetAllContracts_MethodExists()
    {
        // Arrange
        var method = typeof(ContractsController).GetMethod("GetAllContracts");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }

    [Fact]
    public void GetContracts_MethodExists()
    {
        // Arrange
        var method = typeof(ContractsController).GetMethod("GetContracts");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }

    [Fact]
    public void GetContract_MethodExists()
    {
        // Arrange
        var method = typeof(ContractsController).GetMethod("GetContract");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }

    [Fact]
    public void CreateContract_MethodExists()
    {
        // Arrange
        var method = typeof(ContractsController).GetMethod("CreateContract");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }

    [Fact]
    public void RecordPayment_MethodExists()
    {
        // Arrange
        var method = typeof(ContractsController).GetMethod("RecordPayment");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }

    [Fact]
    public void TerminateContract_MethodExists()
    {
        // Arrange
        var method = typeof(ContractsController).GetMethod("TerminateContract");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IActionResult>));
    }
}
