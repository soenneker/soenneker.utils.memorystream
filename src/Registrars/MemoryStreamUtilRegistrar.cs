using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Utils.MemoryStream.Abstract;

namespace Soenneker.Utils.MemoryStream.Registrars;

public static class MemoryStreamUtilRegistrar
{
    /// <summary>
    /// Adds IMemoryStreamUtil as a singleton. <para/>
    /// Shorthand for <code>services.TryAddSingleton</code>
    /// </summary>
    public static void AddMemoryStreamUtil(this IServiceCollection services)
    {
        services.TryAddSingleton<IMemoryStreamUtil, MemoryStreamUtil>();
    }
}