using MinimalHtml.Sample.Filters;
using MinimalHtml.Sample.Layouts;

namespace MinimalHtml.Sample.Pages
{
    public class Lit
    {
        private static readonly Template s_body = static page => page.Html($"""
               <h2>Lit</h2>
               <p>
                   Client-side rendering libraries such as Lit can be easily integrated
               </p>
               <my-element></my-element>
               """);
        private static readonly Template s_head = static page => page.Html($"{Assets.Script:Pages/Lit.ts}");
        
        public static void Map(IEndpointRouteBuilder builder) => builder
            .MapGet("/Lit", static () => Results.Extensions.WithLayout(s_body, s_head))
            .WithSwr();
    }
}
