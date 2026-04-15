using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace NanoMediator;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the NanoMediator pipeline:
    /// <list type="bullet">
    ///   <item><see cref="ISender"/> (scoped — resolves handlers from the current scope)</item>
    ///   <item>All <see cref="IRequestHandler{TRequest,TResponse}"/> implementations found in the given assemblies</item>
    /// </list>
    /// </summary>
    public static IServiceCollection AddNanoMediator(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
            throw new ArgumentException(
                "At least one assembly must be provided for handler scanning.",
                nameof(assemblies));

        services.AddScoped<Sender>();
        services.AddScoped<ISender>(sp => sp.GetRequiredService<Sender>());
        services.AddScoped<IMediator>(sp => sp.GetRequiredService<Sender>());

        foreach (var assembly in assemblies)
        {
            RegisterHandlers(services, assembly);
        }

        return services;
    }

    /// <summary>
    /// Registers an open-generic pipeline behavior.
    /// Behaviors execute in registration order (first registered = outermost).
    /// </summary>
    public static IServiceCollection AddPipelineBehavior(
        this IServiceCollection services,
        Type openGenericBehavior)
    {
        if (!openGenericBehavior.IsGenericTypeDefinition)
            throw new ArgumentException(
                $"{openGenericBehavior.Name} must be an open generic type definition.",
                nameof(openGenericBehavior));

        services.AddTransient(typeof(IPipelineBehavior<,>), openGenericBehavior);
        return services;
    }

    /// <summary>
    /// Registers a closed pipeline behavior for a specific request/response pair.
    /// </summary>
    public static IServiceCollection AddPipelineBehavior<TBehavior, TRequest, TResponse>(
        this IServiceCollection services)
        where TBehavior : class, IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        services.AddTransient<IPipelineBehavior<TRequest, TResponse>, TBehavior>();
        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly)
    {
        var handlerInterfaceType = typeof(IRequestHandler<,>);

        var registrations = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType &&
                            i.GetGenericTypeDefinition() == handlerInterfaceType)
                .Select(i => new { ServiceType = i, ImplementationType = t }));

        foreach (var reg in registrations)
        {
            services.AddTransient(reg.ServiceType, reg.ImplementationType);
        }
    }
}
