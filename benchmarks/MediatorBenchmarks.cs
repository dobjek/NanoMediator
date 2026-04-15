using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using NanoMediator;

[MemoryDiagnoser]
[SimpleJob]
public class MediatorBenchmarks
{
    private ServiceProvider _nanoProv = null!;
    private ServiceProvider _mediatRProv = null!;

    [GlobalSetup]
    public void Setup()
    {
        // ── NanoMediator setup ──
        var nanoServices = new ServiceCollection();
        nanoServices.AddNanoMediator(typeof(NanoRequest).Assembly);
        nanoServices.AddPipelineBehavior(typeof(NanoBehavior<,>));
        _nanoProv = nanoServices.BuildServiceProvider();

        // ── MediatR setup ──
        var mediatRServices = new ServiceCollection();
        mediatRServices.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(MediatRRequest).Assembly);
            cfg.AddOpenBehavior(typeof(MediatRBehavior<,>));
        });
        _mediatRProv = mediatRServices.BuildServiceProvider();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _nanoProv?.Dispose();
        _mediatRProv?.Dispose();
    }

    // ═══════════════════════════════════════════════════════════
    // Benchmarks: Send without pipeline behavior
    // ═══════════════════════════════════════════════════════════

    [Benchmark(Description = "NanoMediator — Send (no behavior)")]
    public async Task<string> Nano_Send_NoBehavior()
    {
        using var scope = _nanoProv.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<NanoMediator.ISender>();
        return await sender.Send(new NanoPlainRequest("hello"));
    }

    [Benchmark(Description = "MediatR — Send (no behavior)")]
    public async Task<string> MediatR_Send_NoBehavior()
    {
        using var scope = _mediatRProv.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<MediatR.ISender>();
        return await sender.Send(new MediatRPlainRequest("hello"));
    }

    // ═══════════════════════════════════════════════════════════
    // Benchmarks: Send with one pipeline behavior
    // ═══════════════════════════════════════════════════════════

    [Benchmark(Description = "NanoMediator — Send (1 behavior)")]
    public async Task<string> Nano_Send_WithBehavior()
    {
        using var scope = _nanoProv.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<NanoMediator.ISender>();
        return await sender.Send(new NanoRequest("hello"));
    }

    [Benchmark(Description = "MediatR — Send (1 behavior)")]
    public async Task<string> MediatR_Send_WithBehavior()
    {
        using var scope = _mediatRProv.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<MediatR.ISender>();
        return await sender.Send(new MediatRRequest("hello"));
    }
}


// ═══════════════════════════════════════════════════════════════
// NanoMediator types
// ═══════════════════════════════════════════════════════════════

public record NanoRequest(string Value) : NanoMediator.IRequest<string>;

public record NanoPlainRequest(string Value) : NanoMediator.IRequest<string>;

public class NanoHandler : NanoMediator.IRequestHandler<NanoRequest, string>
{
    public Task<string> Handle(NanoRequest request, CancellationToken ct)
        => Task.FromResult($"Nano:{request.Value}");
}

public class NanoPlainHandler : NanoMediator.IRequestHandler<NanoPlainRequest, string>
{
    public Task<string> Handle(NanoPlainRequest request, CancellationToken ct)
        => Task.FromResult($"Nano:{request.Value}");
}

public class NanoBehavior<TRequest, TResponse> : NanoMediator.IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(
        TRequest request,
        NanoMediator.RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
        => next();
}


// ═══════════════════════════════════════════════════════════════
// MediatR types
// ═══════════════════════════════════════════════════════════════

public record MediatRRequest(string Value) : MediatR.IRequest<string>;

public record MediatRPlainRequest(string Value) : MediatR.IRequest<string>;

public class MediatRHandler : MediatR.IRequestHandler<MediatRRequest, string>
{
    public Task<string> Handle(MediatRRequest request, CancellationToken ct)
        => Task.FromResult($"MediatR:{request.Value}");
}

public class MediatRPlainHandler : MediatR.IRequestHandler<MediatRPlainRequest, string>
{
    public Task<string> Handle(MediatRPlainRequest request, CancellationToken ct)
        => Task.FromResult($"MediatR:{request.Value}");
}

public class MediatRBehavior<TRequest, TResponse> : MediatR.IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(
        TRequest request,
        MediatR.RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
        => next();
}
