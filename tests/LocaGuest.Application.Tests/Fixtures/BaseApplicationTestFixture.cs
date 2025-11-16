using AutoFixture;
using AutoFixture.AutoMoq;

namespace LocaGuest.Application.Tests.Fixtures;

/// <summary>
/// Base test fixture for Application layer tests
/// Provides AutoFixture with AutoMoq for handler testing
/// </summary>
public class BaseApplicationTestFixture
{
    protected IFixture Fixture { get; }

    public BaseApplicationTestFixture()
    {
        Fixture = new Fixture()
            .Customize(new AutoMoqCustomization { ConfigureMembers = true });

        // Configure to avoid circular references
        Fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
            .ToList()
            .ForEach(b => Fixture.Behaviors.Remove(b));

        Fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        // Register custom configurations
        CustomizeFixture();
    }

    protected virtual void CustomizeFixture()
    {
        // Override in derived classes to add custom configurations
    }
}
