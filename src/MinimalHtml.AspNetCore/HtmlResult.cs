using Microsoft.AspNetCore.Http;

namespace MinimalHtml;

public record HtmlResult<T>(T Context, Template<T> Template, int? StatusCode = 200, string? ContentType = "text/html") : IResult, IContentTypeHttpResult, IStatusCodeHttpResult
{
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = StatusCode ?? 200;
        httpContext.Response.ContentType = "text/html";
        var page = (httpContext.Response.BodyWriter, httpContext.RequestAborted);
        var flushResult = await Template(page, Context);
        if (!flushResult.IsCanceled && !page.RequestAborted.IsCancellationRequested)
        {
            await httpContext.Response.BodyWriter.FlushAsync(page.RequestAborted);
        }
    }
}

public record HtmlResult(Template Template, int? StatusCode = 200, string? ContentType = "text/html") : IResult, IContentTypeHttpResult, IStatusCodeHttpResult
{
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = StatusCode ?? 200;
        httpContext.Response.ContentType = ContentType ?? "text/html";
        var page = (httpContext.Response.BodyWriter, httpContext.RequestAborted);

        var flushResult = await Template(page);
        if (!flushResult.IsCanceled)
        {
            await httpContext.Response.BodyWriter.FlushAsync(page.RequestAborted);
        }
    }
}
