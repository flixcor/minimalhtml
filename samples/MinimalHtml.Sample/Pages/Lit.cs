using MinimalHtml.Sample.Filters;
using MinimalHtml.Sample.Layouts;
using MinimalHtml.Lit;

namespace MinimalHtml.Sample.Pages;

public static class Lit
{
    private static readonly Template s_myElement = static w => w.Lit(/*lang=html*/$"""
    <my-element count={5}>Hello from the slot</my-element>
    """);

    private static readonly Template s_body = static page => page.Html($"""
    <h2>Lit</h2>
    <p>
        This lit element is rendered on the server using Jint
    </p>
    {s_myElement}
    """);
    
    private static readonly Template s_head = static page => page.Html($$"""{{Assets.Script(/*vite*/"lit-hydrate.ts")}}{{Assets.Script(/*vite*/"Pages/Lit.ts")}}""");
        
    public static void Map(IEndpointRouteBuilder builder) => builder
        .MapGet("/Lit", static () => Results.WithLayout(s_body, s_head))
        .WithSwr();
}
