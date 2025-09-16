using MinimalHtml;

namespace Microsoft.AspNetCore.Http;

public static class HtmlResultExtensions
{
    public static HtmlResult<T> Html<T>(this IResultExtensions _, T context, Template<T> template, int statusCode = 200)
        => new HtmlResult<T>(context, template, statusCode);

    public static HtmlResult<T> Html<T>(this IResultExtensions _, Template<T> template, T context, int statusCode = 200)
        => new HtmlResult<T>(context, template, statusCode);

    public static HtmlResult Html(this IResultExtensions _, Template template, int statusCode = 200)
        => new HtmlResult(template, statusCode);
}
