using MinimalHtml.Sample.Filters;
using MinimalHtml.Sample.Layouts;
using MinimalHtml.Lit;

namespace MinimalHtml.Sample.Pages;

public static class StaticTag
{
    private static readonly Template s_tags = static w => w.Lit(/*lang=html*/$"""
    <static-tag label="open" count={3}>3 issues</static-tag>
    <static-tag label="merged" count={12}>12 PRs</static-tag>
    """);

    private static readonly Template s_body = static page => page.Html($"""
    <h2>SSR-only Lit</h2>
    <p>
        This page renders <code>&lt;static-tag&gt;</code> on the server with Declarative Shadow DOM.
        No Lit JavaScript is shipped to the browser — the element class is bundled into the SSR
        runtime only.
    </p>
    {s_tags}
    """);

    public static void Map(IEndpointRouteBuilder builder) => builder
        .MapGet("/StaticTag", static () => Results.WithLayout(s_body))
        .WithSwr();
}
