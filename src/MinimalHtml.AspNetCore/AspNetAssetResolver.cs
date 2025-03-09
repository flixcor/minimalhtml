using System.Collections.Immutable;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalHtml.AspNetCore
{
    public class AspNetAssetResolver
    {
        private readonly ValueTask<ImmutableDictionary<string, Asset>> _ours;
        private readonly ValueTask<ImmutableDictionary<string, Asset>> _combined;

        public AspNetAssetResolver(IWebHostEnvironment env, IMemoryCache? cache, GetAssetDictionary? bundler)
        {
            var filename = Path.Combine(AppContext.BaseDirectory, $"{env.ApplicationName}.staticwebassets.endpoints.json");
            _ours = new AspNetManifestResolver(filename).Parse();
            _combined = bundler != null
                ? bundler().Combine(_ours)
                : _ours;
        }

        public static void Register(IServiceCollection services)
        {
            services.AddSingleton(s => new AspNetAssetResolver(s.GetRequiredService<IWebHostEnvironment>(), s.GetService<IMemoryCache>(), null));
        }

        public static void Register(IServiceCollection services, string pathFromWebRoot, Func<string, GetAssetDictionary> getBundler)
        {
            services.AddSingleton(s =>
            {
                var webHostEnvironment = s.GetRequiredService<IWebHostEnvironment>();
                var memoryCache = s.GetService<IMemoryCache>();
                var fileName = webHostEnvironment.WebRootFileProvider?.GetFileInfo(pathFromWebRoot).PhysicalPath ?? pathFromWebRoot;
                var bundler = getBundler(fileName);
                return new AspNetAssetResolver(webHostEnvironment, memoryCache, bundler);
            });
        }

        public ValueTask<ImmutableDictionary<string, Asset>> GetImportMap() => _ours;

        public async ValueTask<Asset> GetAsset(string id)
        {
            var dict = await _combined;
            var trimmed = Assets.TrimUrl(id);
            return dict.TryGetValue(trimmed, out var found)
                ? found
                : new Asset(trimmed, null, []);
        }
    }
}
