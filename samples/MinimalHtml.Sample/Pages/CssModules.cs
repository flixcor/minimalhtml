using MinimalHtml.Sample.Filters;
using MinimalHtml.Sample.Layouts;

namespace MinimalHtml.Sample.Pages
{
    public partial class CssModules
    {
        private static Flushed Head(HtmlWriter page) => page.Html($"{Assets.Style:/Pages/CssModules.module.css}");

        public static void Map(IEndpointRouteBuilder builder) => builder.MapGet("/css-modules", static () => Results.Extensions.WithLayout(page => page.Html($"""
             <h2>Css modules</h2>
             <div class="{Classes.wrapper}">
                 <p>Paragraphs are pink only on this page when the viewporter is narrower than 50rem, using css modules</p>
             </div>
             """), Head))
            .WithEtag()
            .WithSwr()
            .CacheOutput();
    }
}
