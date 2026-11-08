using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;

namespace MinimalHtml.Vite.Lit;

public sealed class LitSsrInfoResolver(string ssrInfoPath, IWebHostEnvironment env, IMemoryCache cache)
{
    private static readonly SemaphoreSlim s_semaphore = new(1, 1);
    public const string DefaultRelativeSsrInfoPath = "wwwroot/.vite/ssr.json";

    public async ValueTask<LitOptions> GetOptions()
    {
        if (cache.TryGetValue<LitOptions>(ssrInfoPath, out var value) && value is { }) return value;
        await s_semaphore.WaitAsync();
        try
        {
            if (cache.TryGetValue(ssrInfoPath, out value) && value is { }) return value;
            value = await LoadUncached();
            var token = env.ContentRootFileProvider.Watch(ssrInfoPath);
            cache.Set(ssrInfoPath, value, expirationToken: token);
            return value;
        }
        finally
        {
            s_semaphore.Release();
        }
    }

    private async Task<LitOptions> LoadUncached()
    {
        var fileInfo = env.ContentRootFileProvider.GetFileInfo(ssrInfoPath);
        if (fileInfo is null || !fileInfo.Exists)
        {
            throw new InvalidOperationException(
                $"Lit SSR sidecar not found at '{ssrInfoPath}'. Did `vite build` run with the @minimalhtml/vite plugin's `lit` option enabled?");
        }

        await using var stream = fileInfo.CreateReadStream();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;
        var serverPath = root.GetProperty("serverPath"u8).GetString()
            ?? throw new InvalidOperationException($"'{ssrInfoPath}' is missing 'serverPath'.");
        var serverModule = root.TryGetProperty("serverModule"u8, out var moduleProp)
            ? moduleProp.GetString() ?? "server.js"
            : "server.js";

        var absolute = Path.IsPathRooted(serverPath)
            ? serverPath
            : Path.GetFullPath(Path.Combine(env.ContentRootPath, serverPath));

        return new LitOptions
        {
            ServerPath = absolute,
            ServerModule = serverModule,
        };
    }
}
