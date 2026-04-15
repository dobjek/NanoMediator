#!/usr/bin/env dotnet run
//
// NanoMediator Sample — Single-file .NET 10 app
//
// Run with:  dotnet run Sample.cs
//
// This sample demonstrates the core features of NanoMediator:
//   1. Defining requests (queries and commands) with IRequest<T>
//   2. Implementing handlers with IRequestHandler<TRequest, TResponse>
//   3. Registering pipeline behaviors (middleware) with IPipelineBehavior
//   4. Dispatching requests through the pipeline via ISender
//
// No .csproj or .sln required — .NET 10 resolves packages from #:package directives.
//

#:package NanoMediator@1.0.0
#:package Microsoft.Extensions.DependencyInjection@10.0.6

using NanoMediator;
using Microsoft.Extensions.DependencyInjection;

// ── 1. Setup DI ─────────────────────────────────────────────────
//
// AddNanoMediator scans the given assembly for all IRequestHandler<,>
// implementations and registers them as transient services.
// ISender and IMediator are registered as scoped (one per scope/request).

var services = new ServiceCollection();
services.AddNanoMediator(typeof(GetUserByIdQuery).Assembly);

// Pipeline behaviors wrap every handler call, executing in registration
// order (first registered = outermost). Use them for cross-cutting
// concerns like logging, validation, authorization, or transaction management.
services.AddPipelineBehavior(typeof(LoggingBehavior<,>));

using var sp = services.BuildServiceProvider();
using var scope = sp.CreateScope();
var sender = scope.ServiceProvider.GetRequiredService<ISender>();

// ── 2. Send a query ─────────────────────────────────────────────
//
// Queries are read operations. sender.Send() resolves the matching
// IRequestHandler, wraps it in any registered pipeline behaviors,
// and invokes the chain.

var user = await sender.Send(new GetUserByIdQuery(42));
Console.WriteLine($"Got user: {user.Name} (Id={user.Id})");

// ── 3. Send a command ───────────────────────────────────────────
//
// Commands are write operations. Same dispatch mechanism as queries —
// NanoMediator doesn't distinguish between them. The separation is
// a convention in your code (CQRS pattern).

var result = await sender.Send(new CreateUserCommand("Alice", "alice@example.com"));
Console.WriteLine($"Created user with Id={result.Id}");

// ── 4. Error handling ───────────────────────────────────────────
//
// Exceptions thrown in handlers propagate through the pipeline.
// Behaviors can catch them (e.g., for logging or error wrapping).

try
{
    await sender.Send(new CreateUserCommand("", "bad"));
}
catch (ValidationException ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}");
}


// ═══════════════════════════════════════════════════════════════
// REQUESTS — Define what you want to do and what you expect back.
//
// Each request implements IRequest<TResponse>.
// Use records for immutability and concise syntax.
// Response types must be reference types (not int, bool, etc.)
// when using open-generic pipeline behaviors with .NET AOT.
// ═══════════════════════════════════════════════════════════════

public record GetUserByIdQuery(int Id) : IRequest<UserResponse>;

public record CreateUserCommand(string Name, string Email) : IRequest<CreateUserResult>;

public record CreateUserResult(int Id);

public record UserResponse(int Id, string Name, string Email);


// ═══════════════════════════════════════════════════════════════
// HANDLERS — One handler per request. Contains business logic.
//
// Each handler implements IRequestHandler<TRequest, TResponse>.
// Handlers are registered as transient (new instance per call).
// NanoMediator discovers them automatically via assembly scanning.
// ═══════════════════════════════════════════════════════════════

public class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, UserResponse>
{
    public Task<UserResponse> Handle(GetUserByIdQuery request, CancellationToken ct)
    {
        // In a real app, this would query a database
        var user = new UserResponse(request.Id, "Bob", "bob@example.com");
        return Task.FromResult(user);
    }
}

public class CreateUserHandler : IRequestHandler<CreateUserCommand, CreateUserResult>
{
    public Task<CreateUserResult> Handle(CreateUserCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Name is required.");

        // In a real app, this would insert into a database
        Console.WriteLine($"  [Handler] Creating user: {request.Name} ({request.Email})");
        return Task.FromResult(new CreateUserResult(123));
    }
}


// ═══════════════════════════════════════════════════════════════
// PIPELINE BEHAVIOR — Middleware that wraps every handler call.
//
// Behaviors implement IPipelineBehavior<TRequest, TResponse>.
// They receive the request and a delegate to the next step in
// the pipeline (either the next behavior or the handler itself).
//
// Call next() to continue the pipeline, or return early to
// short-circuit (e.g., return a cached result or a validation error).
//
// Common use cases:
//   - Logging (before/after)
//   - Validation (reject bad requests before the handler runs)
//   - Authorization (check permissions)
//   - Transaction management (wrap handler in a DB transaction)
//   - Caching (return cached result without calling the handler)
// ═══════════════════════════════════════════════════════════════

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"  [Pipeline] >> {typeof(TRequest).Name}");
        var response = await next();
        Console.WriteLine($"  [Pipeline] << {typeof(TRequest).Name}");
        return response;
    }
}


// ═══════════════════════════════════════════════════════════════
// Simple exception for demo purposes
// ═══════════════════════════════════════════════════════════════

public class ValidationException(string message) : Exception(message);
