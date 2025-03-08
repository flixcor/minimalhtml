using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using MinimalHtml;

namespace Microsoft.AspNetCore.Http;

public static class HtmlResultExtensions
{
    public static IResult Html<T>(this IResultExtensions _, T context, Template<T> template, int statusCode = 200) => new HtmlResult<T>(context, template, statusCode);
    public static IResult Html<T>(this IResultExtensions _, Template<T> template, T context, int statusCode = 200) => new HtmlResult<T>(context, template, statusCode);

    public static IResult Html(this IResultExtensions _, Template template, int statusCode = 200) => new HtmlResult(template, statusCode);

    public static void Map<Css>(this IEndpointRouteBuilder builder) where Css : ICss
    {
        builder.MapGet(Css.Path, HandleRequest);
        static Task HandleRequest(HttpContext context)
        {
            context.Response.Headers.ContentType = "text/css";
            if (context.Request.Query.ContainsKey("v"))
            {
                context.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
            }
            if (context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            {
                Css.RenderUncompressed(context.Response.BodyWriter);
            }
            else
            {
                context.Response.Headers.ContentEncoding = "br";
                Css.RenderBrotli(context.Response.BodyWriter);
            }
            return context.Response.BodyWriter.FlushAsync(context.RequestAborted).AsTask();
        }
    }
}
