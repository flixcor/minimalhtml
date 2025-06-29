using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;

namespace MinimalHtml;

public static class HtmlTemplateExtensions
{
    public static ValueTask<FlushResult> Html(
        this PipeWriter writer,
        CancellationToken token,
        [InterpolatedStringHandlerArgument(nameof(writer), nameof(token))]
        [StringSyntax("Html")]
        ref HtmlTemplateHandler handler
        ) =>
        handler.Result;

    public static ValueTask<FlushResult> Html(
        this PipeWriter writer,
        [InterpolatedStringHandlerArgument(nameof(writer))]
        [StringSyntax("Html")]
        ref HtmlTemplateHandler handler
        ) =>
        handler.Result;

    public static ValueTask<FlushResult> Html(
        this PipeWriter writer,
        IFormatProvider? provider,
        CancellationToken token,
        [InterpolatedStringHandlerArgument(nameof(writer), nameof(provider), nameof(token))]
        [StringSyntax("Html")]
        ref HtmlTemplateHandler handler
        ) =>
        handler.Result;

    public static ValueTask<FlushResult> Html(
        this PipeWriter writer,
        IFormatProvider? provider,
        [InterpolatedStringHandlerArgument(nameof(writer), nameof(provider))]
        [StringSyntax("Html")]
        ref HtmlTemplateHandler handler
        ) =>
        handler.Result;
}

[InterpolatedStringHandler]
public readonly ref struct HtmlTemplateHandler : ITemplateHandler
{
    private readonly TemplateHandler _inner;

    public HtmlTemplateHandler(int literalLength, int formattedCount, PipeWriter page) : this(literalLength, formattedCount, page, null, default)
    {
    }

    public HtmlTemplateHandler(int literalLength, int formattedCount, PipeWriter page, IFormatProvider? formatProvider) : this(literalLength, formattedCount, page, formatProvider, default)
    {
    }

    public HtmlTemplateHandler(int literalLength, int formattedCount, PipeWriter page, CancellationToken token) : this(literalLength, formattedCount, page, null, token)
    {
    }

    public HtmlTemplateHandler(int literalLength, int formattedCount, PipeWriter page, IFormatProvider? formatProvider, CancellationToken token)
    {
        _inner = new TemplateHandler(literalLength, formattedCount, page, TemplateEncoder.Html, formatProvider, token);
    }

    public ValueTask<FlushResult> Result => _inner.Result;

    public void AppendFormatted(Func<ReadOnlySpan<byte>> getBytes) => _inner.AppendFormatted(getBytes);

    public void AppendFormatted(Memory<byte> bytes) => _inner.AppendFormatted(bytes);

    public void AppendFormatted(string? s) => _inner.AppendFormatted(s);

    public void AppendFormatted(Template? template) => _inner.AppendFormatted(template);

    public void AppendFormatted(Template<string> template, string format) => _inner.AppendFormatted(template, format);

    public void AppendFormatted<T>((IAsyncEnumerable<T>, Template<T>) tuple) => _inner.AppendFormatted(tuple);

    public void AppendFormatted<T>((IEnumerable<T>, Template<T>) tuple) => _inner.AppendFormatted(tuple);

    public void AppendFormatted<T>((T T, Template<T> Template) tuple) => _inner.AppendFormatted(tuple);

    public void AppendFormatted<T>((Task<T> Task, Template<T> Template) tuple) => _inner.AppendFormatted(tuple);

    public void AppendFormatted<T>((Template<T> Template, Task<T> Task) tuple) => _inner.AppendFormatted(tuple);

    public void AppendFormatted<T>((Template<T>, IAsyncEnumerable<T>) tuple) => _inner.AppendFormatted(tuple);

    public void AppendFormatted<T>((Template<T>, IEnumerable<T>) tuple) => _inner.AppendFormatted(tuple);

    public void AppendFormatted<T>((Template<T>, T) tuple) => _inner.AppendFormatted(tuple);

    public void AppendFormatted<T>((Template<T>, ValueTask<T>) tuple) => _inner.AppendFormatted(tuple);

    public void AppendFormatted<T>((ValueTask<T> Task, Template<T> Template) tuple) => _inner.AppendFormatted(tuple);

    public void AppendFormatted<TFormattable>(TFormattable? t, string? format = null) where TFormattable : IUtf8SpanFormattable => _inner.AppendFormatted(t, format);

    public void AppendLiteral(string? s) => _inner.AppendLiteral(s);
}
