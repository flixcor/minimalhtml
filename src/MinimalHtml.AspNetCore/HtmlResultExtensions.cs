using MinimalHtml;

namespace Microsoft.AspNetCore.Http;

public static class HtmlResultExtensions
{
    extension(Results)
    {
        public static HtmlResult<T> Html<T>(T context, Template<T> template, int statusCode = 200)
        => new HtmlResult<T>(context, template, statusCode);

        public static HtmlResult<T> Html<T>(Template<T> template, T context, int statusCode = 200)
            => new HtmlResult<T>(context, template, statusCode);

        public static HtmlResult Html(Template template, int statusCode = 200)
            => new HtmlResult(template, statusCode);
    }
}
