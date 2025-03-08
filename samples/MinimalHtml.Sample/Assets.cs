using Microsoft.Extensions.Caching.Memory;
using MinimalHtml.AspNetCore;

namespace MinimalHtml.Sample
{
    public static class Assets
    {
        public static Flushed Script(HtmlWriter page, string id) => Script(page, (id, true));

        public static async Flushed Script(HtmlWriter page, (string id, bool isModule) context)
        {
            var asset = await AssetResolver.Resolve(context.id);
            return await page.Html($"""
             <script 
                 src="{asset.Src}"
                 {IfTrueish("integrity", asset.Integrity)}
                 type="{(context.isModule ? "module" : "")}"
             ></script>                                                                                             
             """);
        }

        public static async Flushed Style(HtmlWriter page, string id)
        {
            var asset = await AssetResolver.Resolve(id);
            return await page.Html($"""
             <link 
                 href="{asset.Src}"
                 rel="stylesheet"
                 {IfTrueish("integrity", asset.Integrity)}
             />                                                                                                 
             """);
        }

        public static async Flushed SvgFavIcon(HtmlWriter page, string id)
        {
            var asset = await AssetResolver.Resolve(id);
            return await page.Html($"""
             <link 
                 href="{asset.Src}"
                 rel="icon"
                 sizes="any" 
                 type="image/svg+xml"
                 {IfTrueish("integrity", asset.Integrity)}
             />                                                                                                 
             """);
        }

        public static async Flushed ServiceWorker(HtmlWriter page, string id)
        {
            var asset = await AssetResolver.Resolve(id);
            return await page.Html($$"""
             <script type="module">
             if ("serviceWorker" in navigator) {
                navigator.serviceWorker.register("{{asset.Src}}", {scope: "/"})
             }
             </script>
             """);
        }
    }

    public static class AssetResolver
    {
        private static AspNetAssetResolver? s_resolver;

        public static void Setup(WebApplication app)
        {
            if (s_resolver != null) throw new Exception("Resolver already set");
            s_resolver = new AspNetAssetResolver(app.Environment, app.Services.GetRequiredService<IMemoryCache>());
        }

        public static ValueTask<Asset> Resolve(string id)
        {
            if (s_resolver == null) return new ValueTask<Asset>(new Asset(TrimUrl(id), null));
            return s_resolver.GetAsset(id);
        }

        internal static string TrimUrl(string s)
        {
            if(s == null) return "";
            if(s.StartsWith('/')) return s;
            var trimmed = s.AsSpan().TrimStart('~').TrimStart('/');
            return $"/{trimmed}";
        }
    }
}
