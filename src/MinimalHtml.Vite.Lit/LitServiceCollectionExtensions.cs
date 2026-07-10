using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalHtml.Vite.Lit;

public static class LitServiceCollectionExtensions
{
    public static IServiceCollection AddLitRenderer(this IServiceCollection services, string? ssrInfoPath = null) =>
        services
            .AddSingleton(s => new LitSsrInfoResolver(
                ssrInfoPath ?? LitSsrInfoResolver.DefaultRelativeSsrInfoPath,
                s.GetRequiredService<IWebHostEnvironment>(),
                s.GetRequiredService<IMemoryCache>()))
            .AddSingleton<ILitRuntime, LitRuntime>();

    public static IApplicationBuilder UseMinimalHtmlVite(this IApplicationBuilder app)
    {
        LitRuntime.Current = app.ApplicationServices.GetRequiredService<ILitRuntime>();
        return app;
    }
}
