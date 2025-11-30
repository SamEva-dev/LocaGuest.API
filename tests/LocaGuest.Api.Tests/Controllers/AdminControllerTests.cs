using FluentAssertions;
using LocaGuest.Api.Controllers;
using LocaGuest.Api.Tests.Fixtures;
using Xunit;

namespace LocaGuest.Api.Tests.Controllers;

public class AdminControllerTests : BaseTestFixture
{
    // Note: AdminController utilise directement DbContext
    // Les vrais tests seront dans les tests d'intégration
    
    [Fact]
    public void AdminController_Exists()
    {
        // Simple test vérifiant que la classe existe
        // Les tests d'intégration testeront la vraie logique
        var controllerType = typeof(AdminController);
        controllerType.Should().NotBeNull();
    }
}
