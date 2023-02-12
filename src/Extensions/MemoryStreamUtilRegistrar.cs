using Microsoft.Extensions.DependencyInjection;
using Soenneker.Utils.MemoryStream.Abstract;

namespace Soenneker.Utils.MemoryStream.Extensions;

public static class MemoryStreamUtilRegistrar
{
    /// <summary>
    /// Adds IMemoryStreamUtil as a singleton. <para/>
    /// Shorthand for <code>services.AddScoped</code>
    /// </summary>
    public static void AddMemoryStreamUtil(this IServiceCollection services)
    {
        services.AddSingleton<IMemoryStreamUtil, MemoryStreamUtil>();
    }
}