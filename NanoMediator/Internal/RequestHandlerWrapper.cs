using Microsoft.Extensions.DependencyInjection;

namespace NanoMediator.Internal;

/// <summary>
/// Type-erased base so the dispatcher can cache wrappers keyed by request type
/// while calling them with a concrete <typeparamref name="TResponse"/>.
/// </summary>
internal abstract class RequestHandlerWrapperBase<TResponse>
{
    public abstract Task<TResponse> Handle(
        object request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}

/// <summary>
/// Strongly-typed wrapper that resolves the handler and builds the behavior pipeline.
/// One instance per (TRequest, TResponse) pair, cached for the lifetime of the app.
/// </summary>
internal sealed class RequestHandlerWrapper<TRequest, TResponse>
    : RequestHandlerWrapperBase<TResponse>
    where TRequest : IRequest<TResponse>
{
    public override Task<TResponse> Handle(
        object request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var typedRequest = (TRequest)request;

        // Resolve the actual handler
        var handler = serviceProvider
            .GetRequiredService<IRequestHandler<TRequest, TResponse>>();

        // Terminal delegate — the handler itself
        RequestHandlerDelegate<TResponse> terminal =
            () => handler.Handle(typedRequest, cancellationToken);

        // Resolve all pipeline behaviors in registration order
        var behaviors = serviceProvider
            .GetServices<IPipelineBehavior<TRequest, TResponse>>()
            .Reverse()
            .ToArray();

        // Wrap behaviors around the terminal (reverse so first-registered is outermost)
        var pipeline = terminal;
        foreach (var behavior in behaviors)
        {
            var next = pipeline; // capture for closure
            var b = behavior;
            pipeline = () => b.Handle(typedRequest, next, cancellationToken);
        }

        return pipeline();
    }
}
