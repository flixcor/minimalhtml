using Microsoft.Extensions.Caching.Memory;
using MinimalHtml.AspNetCore;

namespace MinimalHtml.Sample
{
    public static class Assets
    {
        public static Flushed Script(HtmlWriter page, string id) => Script(page, (id, true));

        public static async Flushed Script(HtmlWriter page, (string id, bool isModule) context)
        {
            var asset = await page.GetAsset(context.id);
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
            var asset = await page.GetAsset(id);
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
            var asset = await page.GetAsset(id);
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
            var asset = await page.GetAsset(id);
            return await page.Html($$"""
             <script type="module">
             if ("serviceWorker" in navigator) {
                navigator.serviceWorker.register("{{asset.Src}}", {scope: "/"})
             }
             </script>
             """);
        }
    }
}
