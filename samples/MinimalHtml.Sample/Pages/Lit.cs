using MinimalHtml.Sample.Layouts;

namespace MinimalHtml.Sample.Pages;

public static class Lit
{
    private static readonly Template s_body = static page => page.Html($"""
    <h2>Lit</h2>
    <p>
        This lit element is rendered on the client. Try clicking the button to see the count increase.
    </p>
    <my-element count={5}>Hello from the slot</my-element>
    """);

    private static readonly Template s_head = static page => page.Html($$"""{{Assets.Script(/*vite*/"Pages/Lit.ts")}}""");

    public static void Map(IEndpointRouteBuilder builder) => builder
        .MapGet("/Lit", static () => Results.WithLayout(s_body, s_head));
}
