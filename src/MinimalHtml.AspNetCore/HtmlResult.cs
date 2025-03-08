using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalHtml;

public class HtmlResult<T>(T context, Template<T> template, int statusCode = 200): IResult
{
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "text/html";
        var page = new HtmlWriter(httpContext.Response.BodyWriter, httpContext.RequestAborted);
        var flushResult = await template(page, context);
        if (!flushResult.IsCanceled && !page.Token.IsCancellationRequested)
        {
            await httpContext.Response.BodyWriter.FlushAsync(page.Token);
        }
    }
}

public class HtmlResult(Template template, int statusCode = 200): IResult
{
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "text/html";
        var page = new HtmlWriter(httpContext.Response.BodyWriter, httpContext.RequestAborted);

        var flushResult = await template(page);
        if (!flushResult.IsCanceled)
        {
            await httpContext.Response.BodyWriter.FlushAsync(page.Token);
        }
    }
}
