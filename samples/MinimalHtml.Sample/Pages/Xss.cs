using MinimalHtml.Sample.Filters;
using MinimalHtml.Sample.Layouts;

namespace MinimalHtml.Sample.Pages
{
    public class Xss
    {
        private static ReadOnlySpan<byte> UnsafeText() => /*language=html*/ """<script>alert("oops, we've been hacked!")</script>"""u8;

        public static void Map(IEndpointRouteBuilder builder) => builder.MapGet("/xss", static () => Results.Extensions.WithLayout(page => page.Html($"""
             <h2>Cross site scripting (XSS)</h2>
             <p>Strings are escaped by default, so this unsafe text is not interpreted as html:</p>
             <p><code>{UnsafeText}</code></p>
             """)))
            .WithEtag()
            .WithSwr()
            .CacheOutput();
    }
}
