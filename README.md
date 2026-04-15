# NanoMediator

Lightweight CQRS mediator with pipeline behaviors for .NET — a drop-in MediatR replacement with identical interface names. ~130 lines of code, zero magic.

[![Build](https://github.com/dobjek/NanoMediator/actions/workflows/cicd.yml/badge.svg)](https://github.com/dobjek/NanoMediator/actions/workflows/cicd.yml)
[![NuGet](https://img.shields.io/nuget/v/NanoMediator.svg)](https://www.nuget.org/packages/NanoMediator)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## Why?

MediatR is great, but it's more than most projects need. NanoMediator gives you the same developer experience with:

- **Same interfaces** — `ISender`, `IMediator`, `IRequest<T>`, `IRequestHandler<TRequest, TResponse>`, `IPipelineBehavior<TRequest, TResponse>`, `RequestHandlerDelegate<T>`
- **~130 lines** — the entire library. Easy to read, debug, and understand
- **Pipeline behaviors** — same middleware pattern, same registration order semantics
- **Cached handler resolution** — one `MakeGenericType` per request type, cached forever
- **No reflection at dispatch time** — handler wrappers are compiled once and reused
- **Single dependency** — `Microsoft.Extensions.DependencyInjection.Abstractions`

## Install

```bash
dotnet add package NanoMediator
```

**Requirements:** .NET 10+

## Quick Start

### 1. Define a request and handler

```csharp
using NanoMediator;

public record GetUserByIdQuery(int Id) : IRequest<UserResponse>;

public class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, UserResponse>
{
    public async Task<UserResponse> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        // your logic here
        return new UserResponse { Id = request.Id, Name = "Alice" };
    }
}
```

### 2. Register in DI

```csharp
using NanoMediator;

builder.Services.AddNanoMediator(typeof(GetUserByIdHandler).Assembly);
```

This scans the assembly for all `IRequestHandler<,>` implementations and registers them as transient services. `ISender` and `IMediator` are registered as scoped.

### 3. Send requests

```csharp
app.MapGet("/users/{id}", async (int id, ISender sender) =>
{
    var user = await sender.Send(new GetUserByIdQuery(id));
    return Results.Ok(user);
});
```

You can inject either `ISender` or `IMediator` — they resolve to the same instance.

## Pipeline Behaviors

Behaviors wrap every request, executing in registration order (first registered = outermost).

### Define a behavior

```csharp
using NanoMediator;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Handling {typeof(TRequest).Name}");
        var response = await next();
        Console.WriteLine($"Handled {typeof(TRequest).Name}");
        return response;
    }
}
```

### Register behaviors

```csharp
// Open generic (applies to all requests)
builder.Services.AddPipelineBehavior(typeof(LoggingBehavior<,>));

// Or closed generic (applies to specific request/response pair)
builder.Services.AddPipelineBehavior<
    ValidationBehavior<CreateUserCommand, Result>,
    CreateUserCommand,
    Result>();
```

Behaviors execute in registration order. To short-circuit, return without calling `next()`.

## Migrating from MediatR

Three changes:

### 1. Swap the package

```diff
- <PackageReference Include="MediatR" Version="12.5.0" />
- <PackageReference Include="MediatR.Contracts" Version="2.0.1" />
+ <PackageReference Include="NanoMediator" Version="1.0.0" />
```

### 2. Swap the using

```diff
- using MediatR;
+ using NanoMediator;
```

### 3. Swap the DI registration

```diff
- services.AddMediatR(cfg =>
- {
-     cfg.RegisterServicesFromAssembly(assembly);
-     cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
- });
+ services.AddNanoMediator(assembly);
+ services.AddPipelineBehavior(typeof(ValidationBehavior<,>));
```

That's it. Your handlers, requests, and behaviors don't change — the interface names and signatures are identical.

## What's Not Included

NanoMediator intentionally omits features most projects don't use:

- **`INotification` / `INotificationHandler`** — publish/subscribe pattern. If you need it, use events or a message bus.
- **`IStreamRequest`** — streaming responses. Use `IAsyncEnumerable` directly.
- **Source generators** — handler registration uses reflection at startup (once). For ~100 handlers this takes < 1ms.
- **`TypeEvaluator` / handler filtering** — register specific handlers manually if you need selective scanning.

## API Reference

| Type | Description |
|---|---|
| `IRequest<TResponse>` | Marker interface for requests |
| `IRequestHandler<TRequest, TResponse>` | Handles a request and returns a response |
| `ISender` | Dispatches requests through the pipeline |
| `IMediator` | Extends `ISender` (MediatR compatibility) |
| `IPipelineBehavior<TRequest, TResponse>` | Middleware that wraps handler execution |
| `RequestHandlerDelegate<TResponse>` | Delegate representing the next pipeline step |
| `AddNanoMediator(assemblies)` | Scans assemblies and registers handlers + sender |
| `AddPipelineBehavior(type)` | Registers an open-generic pipeline behavior |

## Contributing

1. Fork the repo
2. Create a feature branch (`feature/my-change`)
3. Open a PR against `main`
4. CI must pass (build + tests)

To publish a new version, merge a `release/x.y.z` branch into `main` — CI extracts the version from the branch name and pushes to nuget.org automatically.

## License

[MIT](LICENSE)
