using MinimalHtml.Sample.Layouts;

namespace MinimalHtml.Sample.Pages;
using static TemplateHelpers;

public static class OptimisticUpdates
{
    public static void Map(IEndpointRouteBuilder builder) => builder.MapGet("/optimistic-updates", static (string? name) => Results.WithLayout(Page, name, Assets.Script(/*vite*/"/Components/optimistic-update.ts")));
    
    private static readonly Template<string> Greeter = (page, name) => page.Html($"""
        <p>Hello, {name}!</p>
        <a href="/optimistic-updates">Go back</a>
    """);

    private static readonly Template Form = (page) => page.Html($"""
        <optimistic-update>
            <form method="get">
                <input required type="text" name="name" placeholder="Enter your name" />
                <button type="submit">Submit</button>
            </form>
            <template>
                <p>Hello, <span data-bind="name"></span>!</p>
                <a href="/optimistic-updates">Go back</a>
            </template>
        </optimistic-update>
    """);

    private static readonly Template<string?> Page = (page, name) => page.Html($"""
        <h2>Optimistic updates</h2>
        <p>
            This page demonstrates how to implement optimistic updates with MinimalHtml.
        </p>
        <p>
            When you click the button below, the UI will immediately reflect the change, while the server processes the request in the background.
        </p>
        {(name, IfNotNullOrWhiteSpace(Greeter, Form))}
    """);
}
