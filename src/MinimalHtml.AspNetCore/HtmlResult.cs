using Microsoft.AspNetCore.Http;

namespace MinimalHtml;

/// <summary>
/// An <see cref="IResult"/> that renders HTML using a <see cref="Template"/>. The template is executed and flushed to the response stream when the result is executed. This allows for efficient rendering of large HTML documents without buffering the entire output in memory. The template can also be asynchronous, allowing for streaming of data as it becomes available.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="Context"></param>
/// <param name="Template"></param>
/// <param name="StatusCode"></param>
/// <param name="ContentType"></param>
public record HtmlResult<T>(T Context, Template<T> Template, int? StatusCode = 200, string? ContentType = "text/html") : IResult, IContentTypeHttpResult, IStatusCodeHttpResult
{
    /// <inheritdoc/>
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

/// <summary>
/// An <see cref="IResult"/> that renders HTML using a <see cref="Template"/>. The template is executed and flushed to the response stream when the result is executed. This allows for efficient rendering of large HTML documents without buffering the entire output in memory. The template can also be asynchronous, allowing for streaming of data as it becomes available.
/// </summary>
/// <param name="Template"></param>
/// <param name="StatusCode"></param>
/// <param name="ContentType"></param>
public record HtmlResult(Template Template, int? StatusCode = 200, string? ContentType = "text/html") : IResult, IContentTypeHttpResult, IStatusCodeHttpResult
{
    /// <inheritdoc/>
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = StatusCode ?? 200;
        httpContext.Response.ContentType = ContentType ?? "text/html";
        var flushResult = await Template(httpContext.Response.BodyWriter);
        if (!flushResult.IsCanceled)
        {
            await httpContext.Response.BodyWriter.FlushAsync(httpContext.RequestAborted);
        }
    }
}
