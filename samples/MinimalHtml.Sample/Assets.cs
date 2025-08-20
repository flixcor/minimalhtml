using MinimalHtml.AspNetCore;

namespace MinimalHtml.Sample
{
    public static class Assets
    {
        private static GetAsset s_getAsset = MinimalHtml.Assets.Noop;

        public static void Initialize(WebApplication app) => s_getAsset = app.Services.GetService<GetAsset>() ?? MinimalHtml.Assets.Noop;

        public static Flushed Script(HtmlWriter page, string id) => Script(page, (id, true));

        public static async Flushed Script(HtmlWriter page, (string id, bool isModule) context)
        {
            var asset = await s_getAsset(context.id);
            return await page.Html($"""
             {(asset.Imports, Preload)}
             <script 
                 src="{asset.Src}"
                 {IfTrueish("integrity", asset.Integrity)}
                 type="{(context.isModule ? "module" : "")}"
             ></script>                                                                                             
             """);
        }

        public static async Flushed Style(HtmlWriter page, string id)
        {
            var asset = await s_getAsset(id);
            return await page.Html($"""
             {(asset.Imports, Preload)}
             <link 
                 href="{asset.Src}"
                 rel="stylesheet"
                 {IfTrueish("integrity", asset.Integrity)}
             />                                                                                                 
             """);
        }

        public static async Flushed SvgFavIcon(HtmlWriter page, string id)
        {
            var asset = await s_getAsset(id);
            return await page.Html($"""
             {(asset.Imports, Preload)}
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
            var asset = await s_getAsset(id);
            return await page.Html($$"""
             {{(asset.Imports, Preload)}}
             <script type="module">
             if ("serviceWorker" in navigator) {
                navigator.serviceWorker.register("{{asset.Src}}", {scope: "/"})
             }
             </script>
             """);
        }

        private static Flushed Preload(HtmlWriter page, Asset asset)
        {
            var span = asset.Src.AsSpan();
            var lastIndex = span.LastIndexOf('.');
            if (lastIndex == -1) return default;
            var ext = span.Slice(lastIndex + 1);
            var (rel, loadAs, cors) = ext switch
            {
                "js" => ("modulepreload", "", ""),
                "woff2" => ("preload", "font", "anonymous"),
                "css" => ("preload", "style", ""),
                _ => ("preload", "image", "")
            };
            return page.Html($"""
                {(asset.Imports, Preload)}
                <link href="{asset.Src}" rel="{rel}" {IfTrueish("as", loadAs)} {IfTrueish("crossorigin", cors)} />
                """);
        }
    }
}
