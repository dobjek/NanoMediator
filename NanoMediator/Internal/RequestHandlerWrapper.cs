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
///
/// Optimizations:
/// - Zero-behavior fast path: calls handler directly, no array or closure allocations.
/// - Behaviors resolved once per call via GetServices (no Reverse/ToArray).
/// - Pipeline built by iterating behaviors in reverse index order to avoid LINQ allocations.
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

        var handler = serviceProvider
            .GetRequiredService<IRequestHandler<TRequest, TResponse>>();

        // Resolve behaviors into a list — avoids Reverse() + ToArray() LINQ allocations.
        // GetServices returns them in registration order (outermost first).
        var behaviors = serviceProvider
            .GetServices<IPipelineBehavior<TRequest, TResponse>>();

        // Use IReadOnlyList if available (Microsoft DI returns a List<T>),
        // otherwise fall back to materialization.
        var list = behaviors as IReadOnlyList<IPipelineBehavior<TRequest, TResponse>>
            ?? [.. behaviors];

        // Fast path: no behaviors registered — call handler directly, zero closures.
        if (list.Count == 0)
            return handler.Handle(typedRequest, cancellationToken);

        // Build pipeline by walking behaviors in reverse (last registered = innermost).
        // Terminal delegate is the handler itself.
        RequestHandlerDelegate<TResponse> pipeline =
            () => handler.Handle(typedRequest, cancellationToken);

        for (var i = list.Count - 1; i >= 0; i--)
        {
            var behavior = list[i];
            var next = pipeline;
            pipeline = () => behavior.Handle(typedRequest, next, cancellationToken);
        }

        return pipeline();
    }
}
