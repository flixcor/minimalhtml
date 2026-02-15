using System.Buffers;
using System.Collections.Immutable;
using System.IO.Pipelines;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalHtml.Vite;

public static class ViteManifestExtensions
{
    public static IServiceCollection RegisterViteAssets(this IServiceCollection services, string? manifestPath = null, string? importmapPath = null) =>
        services.AddSingleton(s => new ViteManifestResolver(
            manifestPath ?? ViteManifestResolver.DefaultRelativeManifestPath,
            importmapPath,
            s.GetRequiredService<IWebHostEnvironment>(),
            s.GetRequiredService<IMemoryCache>()))
        .AddSingleton<GetAsset>(s => s.GetRequiredService<ViteManifestResolver>().GetAsset)
        .AddSingleton<WriteImportMap>(s => s.GetRequiredService<ViteManifestResolver>().WriteImportMap);
}

public delegate ValueTask<FlushResult> WriteImportMap((PipeWriter, CancellationToken) tup);

public class ViteManifestResolver(string manifestPath, string? importmapPath, IWebHostEnvironment env, IMemoryCache cache)
{
    private static readonly SemaphoreSlim s_semaphore = new(1, 1);
    public const string DefaultRelativeManifestPath = ".vite/manifest.json";

    public async ValueTask<Asset> GetAsset(string id)
    {
        var assets = await GetAssetsCached();
        var trimmed = id.TrimStart('~', '/');
        return assets.TryGetValue(trimmed, out var found)
            ? found
            : new Asset(trimmed, null, []);
    }

    public async ValueTask<FlushResult> WriteImportMap((PipeWriter write, CancellationToken token) tup)
    {
        var (writer, token) = tup;
        var bytes = await GetImportmapBytesCached(token);
        if (bytes.Length <= 0) return new();
        writer.Write("""<script type="importmap">"""u8);
        writer.Write(bytes);
        writer.Write("""</script>"""u8);
        return new();
    }

    private async Task<byte[]> GetImportmapBytesCached(CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(importmapPath)) return [];
        if (cache.TryGetValue<byte[]>(importmapPath, out var value) && value is { }) return value;
        await s_semaphore.WaitAsync(token);
        try
        {
            if (cache.TryGetValue(manifestPath, out value) && value is { }) return value;
            value = await GetImortmapBytes(token);
            var expirationToken = env.WebRootFileProvider.Watch(manifestPath);
            cache.Set(manifestPath, value, expirationToken);
            return value;
        }
        finally
        {
            s_semaphore.Release();
        }
    }

    private async Task<byte[]> GetImortmapBytes(CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(importmapPath)) return [];
        var path = env.WebRootFileProvider.GetFileInfo(importmapPath);
        if (path == null) return [];
        await using var stream = path.CreateReadStream();
        await using var mem = new MemoryStream();
        await stream.CopyToAsync(mem, token);
        return mem.ToArray();
    }

    private async ValueTask<ImmutableDictionary<string, Asset>> GetAssetsCached()
    {
        if (cache.TryGetValue<ImmutableDictionary<string, Asset>>(manifestPath, out var value) && value is { }) return value;
        await s_semaphore.WaitAsync();
        try
        {
            if (cache.TryGetValue(manifestPath, out value) && value is { }) return value;
            value = await GetAssets();
            var token = env.WebRootFileProvider.Watch(manifestPath);
            cache.Set(manifestPath, value, expirationToken: token);
            return value;
        }
        finally
        {
            s_semaphore.Release();
        }
    }

    private async ValueTask<ImmutableDictionary<string, Asset>> GetAssets()
    {
        var result = new Dictionary<string, Asset>();
        var importDict = new Dictionary<string, Asset>();
        var path = env.WebRootFileProvider.GetFileInfo(manifestPath);
        if (path is null) return ImmutableDictionary<string, Asset>.Empty;
        await using var file = path.CreateReadStream();
        using var doc = await JsonDocument.ParseAsync(file);

        var props = doc.RootElement.EnumerateObject().ToDictionary(x => x.Name, x => x.Value);

        while (props.Count > 0)
        {
            var first = props.FirstOrDefault();
            props.Remove(first.Key);
            var isEntry = first.Value.TryGetProperty("isEntry"u8, out var e) && e.GetBoolean();
            var src = first.Value.GetProperty("file"u8).GetString() ?? first.Key;
            Asset asset;
            if (src.EndsWith(".module.js") && first.Value.GetProperty("src"u8).GetString()?.EndsWith(".module.css") == true)
            {
                asset = HandleImports(first.Value)[0];
            }
            else
            {
                asset = new Asset(Assets.TrimUrl(src), null, HandleImports(first.Value));
            }
            importDict[first.Key] = asset;
            if (isEntry || (!src.EndsWith(".js") && !src.EndsWith(".css")))
            {
                result[first.Key] = asset;
            }
        }

        return result.ToImmutableDictionary();

        ImmutableArray<Asset> HandleImports(JsonElement element)
        {
            var list = new List<Asset>();
            if (element.TryGetProperty("imports"u8, out var imports))
            {
                foreach (var import in imports.EnumerateArray())
                {
                    list.Add(GetImport(import.GetString()));
                }
            }
            if (element.TryGetProperty("assets"u8, out imports))
            {
                foreach (var import in imports.EnumerateArray())
                {
                    list.Add(GetImport(import.GetString()));
                }
            }
            if (element.TryGetProperty("css"u8, out imports))
            {
                foreach (var import in imports.EnumerateArray())
                {
                    list.Add(GetImport(import.GetString()));
                }
            }
            return [.. list];
        }

        Asset GetImport(string importKey)
        {
            if (importDict.TryGetValue(importKey, out var importedAsset))
            {
                return importedAsset;
            }

            if (props.TryGetValue(importKey, out var import))
            {
                importedAsset = new Asset(Assets.TrimUrl(importKey), null, HandleImports(import));
                props.Remove(importKey);
                var isEntry = import.TryGetProperty("isEntry"u8, out var e) && e.GetBoolean();
                if (isEntry)
                {
                    result[importKey] = importedAsset;
                }
            }
            else
            {
                importedAsset = new Asset(Assets.TrimUrl(importKey), null, []);
            }

            importDict[importKey] = importedAsset;
            return importedAsset;
        }
    }
}
