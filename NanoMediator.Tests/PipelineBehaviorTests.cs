using Microsoft.Extensions.DependencyInjection;
using NanoMediator;

namespace NanoMediator.Tests;

// ── Test behaviors ──────────────────────────────────────────────

/// <summary>
/// Appends "[Before1]" and "[After1]" around the pipeline.
/// </summary>
public class OuterBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public static readonly List<string> Log = [];

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        Log.Add("Before1");
        var response = await next();
        Log.Add("After1");
        return response;
    }
}

/// <summary>
/// Appends "[Before2]" and "[After2]" around the pipeline.
/// </summary>
public class InnerBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public static readonly List<string> Log = [];

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        Log.Add("Before2");
        var response = await next();
        Log.Add("After2");
        return response;
    }
}

/// <summary>
/// Short-circuits the pipeline, never calls next.
/// </summary>
public class ShortCircuitBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Return default without calling next
        return Task.FromResult(default(TResponse)!);
    }
}

// ── Tests ───────────────────────────────────────────────────────

[TestFixture]
public class PipelineBehaviorTests
{
    [SetUp]
    public void Setup()
    {
        OuterBehavior<Ping, string>.Log.Clear();
        InnerBehavior<Ping, string>.Log.Clear();
    }

    [Test]
    public async Task Behaviors_ExecuteInRegistrationOrder()
    {
        var services = new ServiceCollection();
        services.AddNanoMediator(typeof(PingHandler).Assembly);
        services.AddPipelineBehavior(typeof(OuterBehavior<,>));
        services.AddPipelineBehavior(typeof(InnerBehavior<,>));
        using var sp = services.BuildServiceProvider();

        var sender = sp.GetRequiredService<ISender>();
        var result = await sender.Send(new Ping("test"));

        // Outer wraps Inner wraps Handler
        Assert.That(result, Is.EqualTo("Pong: test"));
        Assert.That(OuterBehavior<Ping, string>.Log, Is.EqualTo(new[] { "Before1", "After1" }));
        Assert.That(InnerBehavior<Ping, string>.Log, Is.EqualTo(new[] { "Before2", "After2" }));
    }

    [Test]
    public async Task ShortCircuit_PreventsHandlerExecution()
    {
        var services = new ServiceCollection();
        services.AddNanoMediator(typeof(PingHandler).Assembly);
        services.AddPipelineBehavior(typeof(ShortCircuitBehavior<,>));
        using var sp = services.BuildServiceProvider();

        var sender = sp.GetRequiredService<ISender>();
        var result = await sender.Send(new Ping("should not reach handler"));

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task NoBehaviors_HandlerStillWorks()
    {
        var services = new ServiceCollection();
        services.AddNanoMediator(typeof(PingHandler).Assembly);
        // No behaviors registered
        using var sp = services.BuildServiceProvider();

        var sender = sp.GetRequiredService<ISender>();
        var result = await sender.Send(new Ping("direct"));

        Assert.That(result, Is.EqualTo("Pong: direct"));
    }
}
