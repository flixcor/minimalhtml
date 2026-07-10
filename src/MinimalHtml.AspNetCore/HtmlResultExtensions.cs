using MinimalHtml;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Extension methods for creating <see cref="HtmlResult"/> instances from <see cref="Results"/>. These methods provide a convenient way to create HTML results by specifying a template and an optional context, status code, and content type. The resulting <see cref="HtmlResult"/> can then be returned from an endpoint to render HTML content efficiently without buffering the entire output in memory. The templates can also be asynchronous, allowing for streaming of data as it becomes available.
/// </summary>
public static class HtmlResultExtensions
{
    extension(Results)
    {
        /// <summary>
        /// Creates an <see cref="HtmlResult{T}"/> with the specified context, template, and status code. The resulting <see cref="HtmlResult{T}"/> can be returned from an endpoint to render HTML content efficiently without buffering the entire output in memory. The template can also be asynchronous, allowing for streaming of data as it becomes available.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="template"></param>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        public static HtmlResult<T> Html<T>(T context, Template<T> template, int statusCode = 200)
        => new HtmlResult<T>(context, template, statusCode);

        /// <summary>
        /// Creates an <see cref="HtmlResult"/> with the specified template and status code. The resulting <see cref="HtmlResult"/> can be returned from an endpoint to render HTML content efficiently without buffering the entire output in memory. The template can also be asynchronous, allowing for streaming of data as it becomes available.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="template"></param>
        /// <param name="context"></param>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        public static HtmlResult<T> Html<T>(Template<T> template, T context, int statusCode = 200)
            => new HtmlResult<T>(context, template, statusCode);

        /// <summary>
        /// Creates an <see cref="HtmlResult"/> with the specified template and status code. The resulting <see cref="HtmlResult"/> can be returned from an endpoint to render HTML content efficiently without buffering the entire output in memory. The template can also be asynchronous, allowing for streaming of data as it becomes available.
        /// </summary>
        /// <param name="template"></param>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        public static HtmlResult Html(Template template, int statusCode = 200)
            => new HtmlResult(template, statusCode);
    }
}
