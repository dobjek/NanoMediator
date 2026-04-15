namespace NanoMediator;

/// <summary>
/// Combines <see cref="ISender"/> for convenience.
/// MediatR compatibility — resolves from DI as either <see cref="ISender"/> or <see cref="IMediator"/>.
/// </summary>
public interface IMediator : ISender
{
}
