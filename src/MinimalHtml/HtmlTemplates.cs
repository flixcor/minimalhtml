using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;

namespace MinimalHtml;

public static class HtmlTemplateExtensions
{
    public static ValueTask<FlushResult> Html(
        this PipeWriter writer,
        [InterpolatedStringHandlerArgument(nameof(writer))]
        [StringSyntax("Html")]
        ref TemplateHandler handler
        ) =>
        handler.Result;

    public static ValueTask<FlushResult> Html(
        this PipeWriter writer,
        IFormatProvider? provider,
        [InterpolatedStringHandlerArgument(nameof(writer), nameof(provider))]
        [StringSyntax("Html")]
        ref TemplateHandler handler
        ) =>
        handler.Result;
}
