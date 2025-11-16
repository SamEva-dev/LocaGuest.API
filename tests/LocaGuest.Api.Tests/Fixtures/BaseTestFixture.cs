using AutoFixture;
using AutoFixture.AutoMoq;

namespace LocaGuest.Api.Tests.Fixtures;

public class BaseTestFixture
{
    protected IFixture Fixture { get; }

    public BaseTestFixture()
    {
        Fixture = new Fixture()
            .Customize(new AutoMoqCustomization { ConfigureMembers = true });

        // Configure to avoid circular references
        Fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => Fixture.Behaviors.Remove(b));
        
        Fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        // Register custom builders
        CustomizeFixture();
    }

    protected virtual void CustomizeFixture()
    {
        // Override in derived classes to add custom configurations
    }
}
