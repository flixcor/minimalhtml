using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;

namespace MinimalHtml;

public static class HtmlTemplateExtensions
{
    public static ValueTask<FlushResult> Html(
        this (PipeWriter Page, CancellationToken Token) tuple,
        [InterpolatedStringHandlerArgument(nameof(tuple))]
        [StringSyntax("Html")]
        ref HtmlTemplateHandler handler
        ) =>
        handler.Result;

    public static ValueTask<FlushResult> Html(
        this (PipeWriter Page, CancellationToken Token) tuple,
        IFormatProvider? provider,
        [InterpolatedStringHandlerArgument(nameof(tuple), nameof(provider))]
        [StringSyntax("Html")]
        ref HtmlTemplateHandler handler
        ) =>
        handler.Result;
}

[InterpolatedStringHandler]
public readonly ref struct HtmlTemplateHandler : ITemplateHandler
{
    private readonly TemplateHandler _inner;

    public HtmlTemplateHandler(int literalLength, int formattedCount, (PipeWriter Page, CancellationToken Token) tuple) : this(literalLength, formattedCount, tuple, null)
    {
    }

    public HtmlTemplateHandler(int literalLength, int formattedCount, (PipeWriter Page, CancellationToken Token) tuple, IFormatProvider? formatProvider)
    {
        _inner = new TemplateHandler(literalLength, formattedCount, tuple, TemplateEncoder.Html, formatProvider);
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
