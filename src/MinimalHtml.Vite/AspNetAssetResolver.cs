using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

        private static void RegisterGetAsset(IServiceCollection services) => services.TryAddSingleton<GetAsset>(s => s.GetRequiredService<AspNetAssetResolver>().GetAsset);

        public static void Register(IServiceCollection services)
        {
            services.AddSingleton(s => new AspNetAssetResolver(s.GetRequiredService<IWebHostEnvironment>(), s.GetService<IMemoryCache>(), null));
            RegisterGetAsset(services);
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
            RegisterGetAsset(services);
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
