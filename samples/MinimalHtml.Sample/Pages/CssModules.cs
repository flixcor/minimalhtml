using MinimalHtml.Sample.Filters;
using MinimalHtml.Sample.Layouts;

namespace MinimalHtml.Sample.Pages;

public partial class CssModules
{
    private static readonly Template Head = Assets.Style(/*vite*/"/Pages/CssModules.module.css");

    public static void Map(IEndpointRouteBuilder builder) => builder.MapGet("/css-modules", static () => Results.WithLayout(page => page.Html($"""
         <h2>Css modules</h2>
         <div class="{Classes.wrapper}">
             <p>Paragraphs are pink only on this page when the viewporter is narrower than 50rem, using css modules</p>
         </div>
         """), Head))
        .WithSwr()
        .CacheOutput();
}
