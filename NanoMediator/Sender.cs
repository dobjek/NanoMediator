using System.Collections.Concurrent;
using NanoMediator.Internal;

namespace NanoMediator;

/// <summary>
/// Default <see cref="ISender"/> implementation.
/// Resolves the handler for a given request type, wraps it in registered
/// pipeline behaviors, and invokes the pipeline.
///
/// Handler wrappers are cached per request type to avoid repeated
/// generic type construction.
/// </summary>
internal sealed class Sender(IServiceProvider serviceProvider) : IMediator
{
    private static readonly ConcurrentDictionary<Type, object> WrapperCache = new();

    public Task<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();

        var wrapper = (RequestHandlerWrapperBase<TResponse>)WrapperCache.GetOrAdd(
            requestType,
            static t =>
            {
                var responseType = t.GetInterfaces()
                    .First(i => i.IsGenericType &&
                                i.GetGenericTypeDefinition() == typeof(IRequest<>))
                    .GetGenericArguments()[0];

                var wrapperType = typeof(RequestHandlerWrapper<,>)
                    .MakeGenericType(t, responseType);

                return Activator.CreateInstance(wrapperType)!;
            });

        return wrapper.Handle(request, serviceProvider, cancellationToken);
    }
}
