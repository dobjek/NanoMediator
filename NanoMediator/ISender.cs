namespace NanoMediator;

/// <summary>
/// Dispatches a request through the pipeline and returns the response.
/// </summary>
public interface ISender
{
    Task<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default);
}
