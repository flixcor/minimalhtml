using System.IO.Pipelines;
using MinimalHtml.Vite;

namespace MinimalHtml.Sample;

public static class Assets
{
    private static GetAsset s_getAsset = MinimalHtml.Vite.Assets.Noop;

    public static void Initialize(WebApplication app) => s_getAsset = app.Services.GetService<GetAsset>() ?? MinimalHtml.Vite.Assets.Noop;

    public static Template Script(string id) => Script((id, true, false));

    public static Template Script((string id, bool isModule, bool async) context) => async page =>
    {
        var asset = await s_getAsset(context.id);
        return await page.Html($"""
         {(asset.Imports, Preload)}
         <script
             src="{asset.Src}"
             {IfTrueish("integrity", asset.Integrity)}
             {IfTrueish("type", context.isModule ? "module" : null)}
             {IfTrueish("async", context.async)}
         ></script>
         """);
    };

    public static Template Style(string id) => async page =>
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
    };

    public static Template SvgFavIcon(string id) => async page =>
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
    };

    public static Template ServiceWorker(string id) => async page =>
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
    };

    private static readonly Template<Asset> Preload = (page, asset) =>
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
    };
}
