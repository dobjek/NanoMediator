using Microsoft.Extensions.DependencyInjection;
using NanoMediator;

namespace NanoMediator.Tests;

[TestFixture]
public class RegistrationTests
{
    [Test]
    public void AddNanoMediator_RegistersSenderAsScopedService()
    {
        var services = new ServiceCollection();
        services.AddNanoMediator(typeof(PingHandler).Assembly);

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ISender));

        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Scoped));
    }

    [Test]
    public void AddNanoMediator_RegistersMediatorAsScopedService()
    {
        var services = new ServiceCollection();
        services.AddNanoMediator(typeof(PingHandler).Assembly);

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMediator));

        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Scoped));
    }

    [Test]
    public async Task IMediator_ResolvesAndDispatches()
    {
        var services = new ServiceCollection();
        services.AddNanoMediator(typeof(PingHandler).Assembly);
        using var sp = services.BuildServiceProvider();

        var mediator = sp.GetRequiredService<IMediator>();
        var result = await mediator.Send(new Ping("via IMediator"));

        Assert.That(result, Is.EqualTo("Pong: via IMediator"));
    }

    [Test]
    public void AddNanoMediator_RegistersHandlerAsTransient()
    {
        var services = new ServiceCollection();
        services.AddNanoMediator(typeof(PingHandler).Assembly);

        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IRequestHandler<Ping, string>));

        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Transient));
        Assert.That(descriptor.ImplementationType, Is.EqualTo(typeof(PingHandler)));
    }

    [Test]
    public void AddNanoMediator_NoAssemblies_Throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(
            () => services.AddNanoMediator());
    }

    [Test]
    public void AddPipelineBehavior_NonGenericType_Throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(
            () => services.AddPipelineBehavior(typeof(PingHandler)));
    }
}
