
using System.Collections.Immutable;
using System.Reflection;
using MinimalHtml.AspNetCore;
using MinimalHtml.Sample.Components;

namespace MinimalHtml.Sample.Layouts;

public readonly struct LayoutProps<T>
{
    public required T Context { get; init; }
    public required Template<T> Body { get; init; }
    public required Template<string> NavLink { get; init; }
    public required ImmutableDictionary<string, Asset> ImportedAssets { get; init; }
    public Template? Head { get; init; }
}

public readonly struct LayoutProps
{
    public required Template Body { get; init; }
    public required Template<string> NavLink { get; init; }
    public Template? Head { get; init; }
    public required ImmutableDictionary<string, Asset> ImportedAssets { get; init; }
    
    public static implicit operator LayoutProps<Template>(LayoutProps value) => new()
    {
        Context = value.Body,
        Body = static (page, template) => template(page),
        NavLink = value.NavLink,
        Head = value.Head,
        ImportedAssets = value.ImportedAssets
    };
}

public static class DefaultLayout
{
    private class LayoutResult<T>(Template<T> page, T context, Template? head = null, int statusCode = 200) : IResult
    {
        public async Task ExecuteAsync(HttpContext httpContext)
        {
            var navLink = httpContext.RequestServices.GetRequiredService<NavLink>();
            var importedAssets = ImmutableDictionary<string, Asset>.Empty;
            var resolver = httpContext.RequestServices.GetService<AspNetAssetResolver>();
            if (resolver != null)
            {
                importedAssets = await resolver.GetImportMap();
            }
            var props = new LayoutProps<T>{ Body = page, Context = context, NavLink = navLink.Render, Head = head, ImportedAssets = importedAssets };
            await new HtmlResult<LayoutProps<T>>(props, Render, statusCode).ExecuteAsync(httpContext);
        }
    }

    

    public static IResult WithLayout<T>(this IResultExtensions _, Template<T> page, T context, Template? head = null, int statusCode = 200)
        => new LayoutResult<T>(page, context, head, statusCode);
    
    public static IResult WithLayout(this IResultExtensions _, Template page, Template? head = null, int statusCode = 200)
        => new LayoutResult<Template>(static (p,t) => t(p), page, head, statusCode);

    public static Flushed Render(HtmlWriter page, LayoutProps props) => Render<Template>(page, props);
    public static Flushed Render<T>(HtmlWriter page, LayoutProps<T> context) => page.Html($$"""
         <!DOCTYPE html>
         <html lang="en">
         <head>
             <meta charset="utf-8" />
             <meta name="viewport" content="width=device-width, initial-scale=1.0" />
             <meta name="view-transition" content="same-origin" />
                 <!-- the props -->
             {{(ImportMap, context.ImportedAssets)}}
             {{Assets.SvgFavIcon:img/favicon.svg}}
             {{(Assets.Script, ("Layouts/DefaultLayout.ts", false))}}
             {{Assets.Style:Layouts/DefaultLayout.css}}
             {{Assets.ServiceWorker:serviceworker.js}}
             {{context.Head}}
             <script type="speculationrules">
                {
                "prerender": [{
                    "where": {
                    "href_matches": "/*"
                    },
                    "eagerness": "moderate"
                }]
                }
                </script>
         </head>
         <body>
         <template shadowrootmode="open">
             <slot name="header"></slot>
             <slot name="main"></slot>
             <slot name="footer"></slot>
         </template>
         <header slot="header">
            <div class="backdrop"></div>
            <nav>
                <button>Menu</button>
                <ul>
                    <li>
                        <a {{context.NavLink:/}}>Progressive enhancement</a>
                    </li>
                    <li>
                        <a {{context.NavLink:/streaming}}>Streaming</a>
                    </li>
                    <li>
                        <a {{context.NavLink:/xss}}>Cross site scripting</a>
                    </li>
                    <li>
                        <a {{context.NavLink:/forms}}>Forms</a>
                    </li>
                    <li>
                        <a {{context.NavLink:/lit}}>Lit</a>
                    </li>
                    <li>
                        <a {{context.NavLink:/active-search}}>Active search</a>
                    </li>
                    <li>
                        <a {{context.NavLink:/any-order}}>Unordered streaming</a>
                    </li>
                    <li>
                        <a {{context.NavLink:/swr}}>Stale while revalidate</a>
                    </li>
                    <li>
                        <a {{context.NavLink:/css-modules}}>CSS modules</a>
                    </li>
                </ul>
            </nav>
         </header>
         <footer slot="footer" class="the-footer">Version: <version-number></version-number></footer>
         <main role="main" slot="main">
           {{(context.Body,context.Context)}}
         </main>
         </body>
         </html>
         """);

    private static Flushed ImportMap(HtmlWriter page, ImmutableDictionary<string, Asset> importedAssets) => page.Html($$"""
        <script type="importmap">
        {
            "imports": {
            {{(importedAssets.Select((k, i) => (k.Key, k.Value.Src, i == importedAssets.Count - 1)), ImportMapAsset)}}
            }
        }
        </script>
        """);

    private static Flushed ImportMapAsset(HtmlWriter page, (string key, string value, bool last) tup) => page.Html($""" "{tup.key}": "{tup.value}"{(tup.last ? "" : ",")} """);
}
