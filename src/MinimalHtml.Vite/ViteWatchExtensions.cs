using Microsoft.Extensions.DependencyInjection;

namespace MinimalHtml.Vite;

public static class ViteWatchExtensions
{
    /// <summary>
    /// Registers a hosted service that spawns <c>{packageManager} run build:watch</c>
    /// (or your overridden command) as a child process during development, piping its
    /// output through <c>ILogger&lt;ViteWatcher&gt;</c> and killing it on app shutdown.
    /// No-op outside development unless <see cref="ViteWatchOptions.Enabled"/> is left
    /// at <c>true</c> AND <c>IHostEnvironment.IsDevelopment()</c>.
    /// </summary>
    public static IServiceCollection AddViteWatch(
        this IServiceCollection services,
        Action<ViteWatchOptions>? configure = null)
    {
        var builder = services.AddOptions<ViteWatchOptions>();
        if (configure is not null) builder.Configure(configure);
        services.AddHostedService<ViteWatcher>();
        return services;
    }
}
