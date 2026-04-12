using Microsoft.AspNetCore.Http;

namespace MinimalHtml;

public record HtmlResult<T>(T Context, Template<T> Template, int? StatusCode = 200, string? ContentType = "text/html") : IResult, IContentTypeHttpResult, IStatusCodeHttpResult
{
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = StatusCode ?? 200;
        httpContext.Response.ContentType = "text/html";
        var flushResult = await Template(httpContext.Response.BodyWriter, Context);
        if (!flushResult.IsCanceled)
        {
            await httpContext.Response.BodyWriter.FlushAsync(httpContext.RequestAborted);
        }
    }
}

public record HtmlResult(Template Template, int? StatusCode = 200, string? ContentType = "text/html") : IResult, IContentTypeHttpResult, IStatusCodeHttpResult
{
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = StatusCode ?? 200;
        httpContext.Response.ContentType = ContentType ?? "text/html";
        var flushResult = await Template(httpContext.Response.BodyWriter);
        if (!flushResult.IsCanceled)
        {
            await httpContext.Response.BodyWriter.FlushAsync();
        }
    }
}
