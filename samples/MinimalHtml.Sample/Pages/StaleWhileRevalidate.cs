using MinimalHtml.Sample.Filters;
using MinimalHtml.Sample.Layouts;

namespace MinimalHtml.Sample.Pages
{
    public class StaleWhileRevalidate
    {
        public static void Map(IEndpointRouteBuilder builder) => builder.MapGet("/swr", static () => Results.Extensions.WithLayout(p => p.Html($"""
            <h2>Stale-While-Revalidate</h2>
            <p>This page changes every minute on the server. The service worker serves a stale response but checks with the server. 
            If the fresh response has a different ETag header, the user is informed.</p>
            <p>Generated at: {DateTime.Now:yyyy-MM-dd HH:mm z}</p>
            """)))
            .WithEtag()
            .WithSwr();
    }
}
