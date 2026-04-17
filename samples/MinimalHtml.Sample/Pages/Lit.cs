using MinimalHtml.Sample.Filters;
using MinimalHtml.Sample.Layouts;
using MinimalHtml.Lit;

namespace MinimalHtml.Sample.Pages;

public static class Lit
{
    private static readonly Template s_body = static page => page.Html($"""
    <h2>Lit</h2>
    <p>
        This lit element is rendered on the server using Jint
    </p>
    {(page => page.Lit($"<my-element count={5}>Hello from the slot</my-element>"))}
    """);
    private static readonly Template s_head = static page => page.Html($"{Assets.Script:lit-hydrate.ts}{Assets.Script:Pages/Lit.ts}");
        
    public static void Map(IEndpointRouteBuilder builder) => builder
        .MapGet("/Lit", static () => Results.WithLayout(s_body, s_head))
        .WithSwr();
}
