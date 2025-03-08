using System.IO.Pipelines;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticAssets;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;

namespace MinimalHtml.AspNetCore
{
    public delegate Template GetAsset(string id, Template<Asset> render);
    
    public readonly record struct Asset(string Src, string? Integrity);
    
    public class AspNetAssetResolver(IWebHostEnvironment env, IMemoryCache cache)
    {
        private readonly string fileName = Path.Combine(AppContext.BaseDirectory, $"{env.ApplicationName}.staticwebassets.endpoints.json");

        private ValueTask<IReadOnlyDictionary<string, Asset>> GetDictionary()
        {
            var pathFromContentRoot = fileName.Substring(env.ContentRootPath.Length);
            var test = env.ContentRootFileProvider.GetFileInfo(pathFromContentRoot);
            if(cache.TryGetValue(fileName, out IReadOnlyDictionary<string, Asset>? result) && result != null) return new(result);
            return new(GetAsync());
            async Task<IReadOnlyDictionary<string, Asset>> GetAsync()
            {
                var file = await StaticAssetsManifest.Parse(fileName);
                var changeToken = env.ContentRootFileProvider.Watch(pathFromContentRoot);

                // Configure the cache entry options for a five minute
                // sliding expiration and use the change token to
                // expire the file in the cache if the file is
                // modified.
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                    .AddExpirationToken(changeToken);

                // Put the file contents into the cache.
                cache.Set(fileName, file, cacheEntryOptions);
                return file;
            }
        }

        public ValueTask<Asset> GetAsset(string id)
        {
            var task = GetDictionary();
            if (task.IsCompletedSuccessfully) return new(Get(id, task.Result));
            return new(GetAsync(id));
            
            async Task<Asset> GetAsync(string id)
            {
                var collection = await task;
                return Get(id, collection);
            }

            Asset Get(string id, IReadOnlyDictionary<string, Asset> assets)
            {
                id = TrimUrl(id);
                return assets
                    .TryGetValue(id, out var asset) 
                    ? asset 
                    : new Asset(id, null);
            }
        }

        internal static string TrimUrl(string s)
        {
            if(s == null) return "";
            if(s.StartsWith('/')) return s;
            var trimmed = s.AsSpan().TrimStart('~').TrimStart('/');
            return $"/{trimmed}";
        }
        
        public Template Asset(string id, Template<Asset> render)
        {
            var assetTask = GetAsset(id);
            
            return async (page) =>
            {
                var asset = await assetTask;
                return await render(page, asset);
            };
        }
    }
    
    [JsonSerializable(typeof(StaticAssetsManifest))]
    internal partial class StaticAssetsManifestJsonContext : JsonSerializerContext
    {
    }
    
    internal class StaticAssetsManifest
    {
        internal static async Task<IReadOnlyDictionary<string, Asset>> Parse(string manifestPath)
        {
            ArgumentNullException.ThrowIfNull(manifestPath);
            await using var stream = File.OpenRead(manifestPath);
            var manifest = await JsonSerializer.DeserializeAsync(stream, StaticAssetsManifestJsonContext.Default.StaticAssetsManifest);
            var result = new Dictionary<string, Asset>();
            
            foreach (var descriptor in manifest.Endpoints)
            {
                string? label = null;
                string? integrity = null;

                // If there's a selector this means that this is an alternative representation for a resource, so skip it.
                if (descriptor.Selectors.Count == 0)
                {
                    var foundProperties = 0;
                    for (var i = 0; i < descriptor.Properties.Count; i++)
                    {
                        var property = descriptor.Properties[i];
                        if (property.Name.Equals("label", StringComparison.OrdinalIgnoreCase))
                        {
                            label = property.Value;
                            foundProperties++;
                        }

                        else if (property.Name.Equals("integrity", StringComparison.OrdinalIgnoreCase))
                        {
                            integrity = property.Value;
                            foundProperties++;
                        }

                        if (foundProperties == 2)
                        {
                            break;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(label))
                    {
                        var asset = new Asset(AspNetAssetResolver.TrimUrl(descriptor.Route), integrity);
                        result[AspNetAssetResolver.TrimUrl(label)] = asset;
                    }
                }
            }

            return result;
        }

        public List<StaticAssetDescriptor> Endpoints { get; set; } = [];
    }
}
