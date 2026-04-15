using Microsoft.Extensions.DependencyInjection;
using NanoMediator;

namespace NanoMediator.Tests;

// ── Test request/handler types ──────────────────────────────────

public record Ping(string Message) : IRequest<string>;

public class PingHandler : IRequestHandler<Ping, string>
{
    public Task<string> Handle(Ping request, CancellationToken cancellationToken)
        => Task.FromResult($"Pong: {request.Message}");
}

public record NoHandlerRequest : IRequest<string>;

// ── Tests ───────────────────────────────────────────────────────

[TestFixture]
public class SenderTests
{
    private ServiceProvider _sp = null!;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddNanoMediator(typeof(PingHandler).Assembly);
        _sp = services.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown() => _sp?.Dispose();

    [Test]
    public async Task Send_DispatchesToHandler_ReturnsResponse()
    {
        var sender = _sp.GetRequiredService<ISender>();

        var result = await sender.Send(new Ping("hello"));

        Assert.That(result, Is.EqualTo("Pong: hello"));
    }

    [Test]
    public void Send_NullRequest_ThrowsArgumentNullException()
    {
        var sender = _sp.GetRequiredService<ISender>();

        Assert.ThrowsAsync<ArgumentNullException>(
            () => sender.Send<string>(null!));
    }

    [Test]
    public void Send_NoRegisteredHandler_ThrowsInvalidOperationException()
    {
        var sender = _sp.GetRequiredService<ISender>();

        Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.Send(new NoHandlerRequest()));
    }
}
