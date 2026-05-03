using Microsoft.Extensions.DependencyInjection;

namespace MinimalHtml.Lit;

public static class LitServiceCollectionExtensions
{
    /// <summary>
    /// Configures the <see cref="LitRenderer"/> with a server bundle path. Defaults to
    /// <c>{AppContext.BaseDirectory}/dist/server/server.js</c>, matching the output of
    /// <c>@minimalhtml/vite/lit/plugin</c>.
    /// </summary>
    public static IServiceCollection AddLitRenderer(
        this IServiceCollection services,
        string? serverPath = null,
        string? serverModule = null)
    {
        LitRenderer.Setup(new LitOptions
        {
            ServerPath = serverPath ?? Path.Combine(AppContext.BaseDirectory, "dist", "server"),
            ServerModule = serverModule ?? "server.js",
        });
        return services;
    }
}
