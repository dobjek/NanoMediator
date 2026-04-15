namespace NanoMediator;

/// <summary>
/// Marker interface for a request that returns <typeparamref name="TResponse"/>.
/// </summary>
public interface IRequest<out TResponse>
{
}
