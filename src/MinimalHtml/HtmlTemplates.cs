using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;

namespace MinimalHtml;

/// <summary>
/// Extension methods for rendering HTML using interpolated string handlers. These methods allow for efficient rendering of HTML by writing directly to a <see cref="PipeWriter"/> without the need for intermediate string allocations. The <see cref="TemplateHandler"/> is responsible for processing the interpolated string and writing the resulting HTML to the provided <see cref="PipeWriter"/>.
/// </summary>
public static class HtmlTemplateExtensions
{
    /// <summary>
    /// Renders an interpolated string as HTML by writing directly to the provided <see cref="PipeWriter"/>. The <see cref="TemplateHandler"/> processes the interpolated string and produces the resulting HTML, which is then written to the <see cref="PipeWriter"/>. This method allows for efficient rendering of HTML without intermediate string allocations.
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static ValueTask<FlushResult> Html(
        this PipeWriter writer,
        [InterpolatedStringHandlerArgument(nameof(writer))]
        [StringSyntax("Html")]
        ref TemplateHandler handler
        ) =>
        handler.Result;

    /// <summary>
    /// Renders an interpolated string as HTML by writing directly to the provided <see cref="PipeWriter"/>. The <see cref="TemplateHandler"/> processes the interpolated string and produces the resulting HTML, which is then written to the <see cref="PipeWriter"/>. This method allows for efficient rendering of HTML without intermediate string allocations. The provided <see cref="IFormatProvider"/> can be used to control formatting of values within the interpolated string.
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="provider"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static ValueTask<FlushResult> Html(
        this PipeWriter writer,
        IFormatProvider? provider,
        [InterpolatedStringHandlerArgument(nameof(writer), nameof(provider))]
        [StringSyntax("Html")]
        ref TemplateHandler handler
        ) =>
        handler.Result;
}
