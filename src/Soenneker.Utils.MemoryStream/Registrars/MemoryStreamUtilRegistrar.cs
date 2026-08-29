using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Utils.MemoryStream.Abstract;

namespace Soenneker.Utils.MemoryStream.Registrars;

/// <summary>
/// Represents the memory stream util registrar.
/// </summary>
public static class MemoryStreamUtilRegistrar
{
    /// <summary>
    /// Adds IMemoryStreamUtil as a singleton. <para/>
    /// Shorthand for <code>services.TryAddSingleton</code>
    /// </summary>
    /// <returns>Adds IMemoryStreamUtil as a singleton. <para/> Shorthand for <code>services.TryAddSingleton</code>.</returns>
    public static IServiceCollection AddMemoryStreamUtilAsSingleton(this IServiceCollection services)
    {
        services.TryAddSingleton<IMemoryStreamUtil, MemoryStreamUtil>();

        return services;
    }

    /// <summary>
    /// Registers the recyclable memory-stream utility and its manager with scoped lifetime.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddMemoryStreamUtilAsScoped(this IServiceCollection services)
    {
        services.TryAddScoped<IMemoryStreamUtil, MemoryStreamUtil>();

        return services;
    }
}