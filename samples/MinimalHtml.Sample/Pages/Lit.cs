using MinimalHtml.Sample.Filters;
using MinimalHtml.Sample.Layouts;
using MinimalHtml.Vite.Lit;

namespace MinimalHtml.Sample.Pages;

public static class Lit
{
    private static readonly Template s_myElement = static w => w.Lit(/*lang=html*/$"""
    <my-element count={5}>Hello from the slot</my-element>
    """);

    private static readonly Template s_body = static page => page.Html($"""
    <h2>Lit</h2>
    <p>
        This lit element is rendered on the server using Jint, which is a JavaScript engine written in C#. The same code is also hydrated on the client, so you can interact with it. Try clicking the button to see the count increase.
    </p>
    {s_myElement}
    """);
    
    private static readonly Template s_head = static page => page.Html($$"""{{Assets.Script(/*vite*/"virtual:minimal-html/lit-hydrate")}}{{Assets.Script(/*vite*/"Pages/Lit.ts")}}""");
        
    public static void Map(IEndpointRouteBuilder builder) => builder
        .MapGet("/Lit", static () => Results.WithLayout(s_body, s_head))
        .WithSwr();
}
