using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;

namespace MinimalHtml;

public interface ITemplateHandler
{
    void AppendFormatted(Func<ReadOnlySpan<byte>> getBytes);
    void AppendFormatted(ReadOnlyMemory<byte> bytes);
    void AppendFormatted(string? s);
    void AppendFormatted(Template? template);
    void AppendFormatted(Template<string> template, string format);
    void AppendFormatted<T>((IAsyncEnumerable<T>, Template<T>) tuple);
    void AppendFormatted<T>((IEnumerable<T>, Template<T>) tuple);
    void AppendFormatted<T>((T T, Template<T> Template) tuple);
    void AppendFormatted<T>((Task<T> Task, Template<T> Template) tuple);
    void AppendFormatted<T>((Template<T> Template, Task<T> Task) tuple);
    void AppendFormatted<T>((Template<T>, IAsyncEnumerable<T>) tuple);
    void AppendFormatted<T>((Template<T>, IEnumerable<T>) tuple);
    void AppendFormatted<T>((Template<T>, T) tuple);
    void AppendFormatted<T>((Template<T>, ValueTask<T>) tuple);
    void AppendFormatted<T>((ValueTask<T> Task, Template<T> Template) tuple);
    void AppendFormatted<TFormattable>(TFormattable? t, string? format = null) where TFormattable : IUtf8SpanFormattable;
    void AppendLiteral(string? s);
    ValueTask<FlushResult> Result { get; }
}

[InterpolatedStringHandler]
public ref struct TemplateHandler : ITemplateHandler
{
    private static readonly ConcurrentDictionary<string, byte[]> s_buffers = new();
    private readonly PipeWriter _writer;
    private readonly IFormatProvider? _formatProvider;
    private readonly TemplateEncoder _encoder;
    private readonly CancellationToken _token;

    public static void Precompile(string key, byte[] bytes) => s_buffers[key] = bytes;

    public ValueTask<FlushResult> Result { get; private set; } = new();

    public TemplateHandler(int literalLength, int formattedCount, (PipeWriter Page, CancellationToken Token) tuple, TemplateEncoder? encoder, IFormatProvider? formatProvider = null)
    {
        _writer = tuple.Page;
        _formatProvider = formatProvider;
        _encoder = encoder ?? TemplateEncoder.Html;
        _token = tuple.Token;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLiteral(string? s)
    {
        if (!string.IsNullOrWhiteSpace(s) && !_token.IsCancellationRequested)
        {
            Result = Handle(_writer, _encoder, Result, s, static (p, _, s) => p.Write(s_buffers.GetOrAdd(s, Encoding.UTF8.GetBytes)));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(string? s)
    {
        if (!string.IsNullOrEmpty(s) && !_token.IsCancellationRequested)
        {
            Result = Handle(_writer, _encoder, Result, s, static (p, e, s) => e.WriteEncoded(p, s));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(ReadOnlyMemory<byte> bytes)
    {
        if (!bytes.IsEmpty && !_token.IsCancellationRequested)
        {
            Result = Handle(_writer, _encoder, Result, bytes, static (p, e, b) => e.WriteEncoded(p, b.Span));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(Func<ReadOnlySpan<byte>> getBytes)
    {
        if (!_token.IsCancellationRequested)
        {
            Result = Handle(_writer, _encoder, Result, getBytes, static (p, e, b) => e.WriteEncoded(p, b()));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<TFormattable>(TFormattable? t, string? format = null) where TFormattable : IUtf8SpanFormattable
    {
        if (t != null && !_token.IsCancellationRequested)
        {
            Result = Handle(_writer, _encoder, Result, (t, format, _formatProvider), static (p, e, tup) => e.WriteEncoded(p, tup.t, tup.format, tup._formatProvider));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(Template? template)
    {
        if (template is not null && !_token.IsCancellationRequested)
        {
            Result = Handle(_writer, Result, template, _token);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(Template<string> template, string format)
        => AppendFormatted((format, template));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>((Template<T>, T) tuple)
        => AppendFormatted((tuple.Item2, tuple.Item1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>((T T, Template<T> Template) tuple)
    {
        Result = Handle(_writer, Result, tuple.T, tuple.Template, _token);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>((Task<T> Task, Template<T> Template) tuple)
        => AppendFormatted((tuple.Template, tuple.Task));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>((Template<T> Template, Task<T> Task) tuple)
        => AppendFormatted((new ValueTask<T>(tuple.Task), tuple.Template));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>((Template<T>, ValueTask<T>) tuple)
        => AppendFormatted((tuple.Item2, tuple.Item1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>((ValueTask<T> Task, Template<T> Template) tuple)
    {
        Result = Handle(_writer, Result, tuple.Item1, tuple.Item2, _token);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>((Template<T>, IAsyncEnumerable<T>) tuple)
        => AppendFormatted((tuple.Item2, tuple.Item1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>((IAsyncEnumerable<T>, Template<T>) tuple)
    {
        Result = Handle(_writer, Result, tuple.Item1, tuple.Item2, _token);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>((Template<T>, IEnumerable<T>) tuple)
        => AppendFormatted((tuple.Item2, tuple.Item1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>((IEnumerable<T>, Template<T>) tuple)
    {
        Result = Handle(_writer, Result, tuple.Item1, tuple.Item2, _token);
    }

    private static async ValueTask<FlushResult> Handle<T>(IBufferWriter<byte> page, TemplateEncoder encoder, ValueTask<FlushResult> current, T state, Action<IBufferWriter<byte>, TemplateEncoder, T> handler)
    {
        var flushResult = await current.ConfigureAwait(false);
        if (flushResult.IsCompleted || flushResult.IsCanceled) return flushResult;
        handler(page, encoder, state);
        return new();
    }

    private static async ValueTask<FlushResult> Handle(PipeWriter page, ValueTask<FlushResult> current, Template handler, CancellationToken token)
    {
        var flushResult = await current.ConfigureAwait(false);
        if (flushResult.IsCompleted || flushResult.IsCanceled) return flushResult;
        return await handler((page, token));
    }

    private static async ValueTask<FlushResult> Handle<T>(PipeWriter page, ValueTask<FlushResult> current, T state, Template<T> handler, CancellationToken token)
    {
        var flushResult = await current.ConfigureAwait(false);
        if (flushResult.IsCompleted || flushResult.IsCanceled) return flushResult;
        return await handler((page, token), state);
    }

    private static async ValueTask<FlushResult> Handle<T>(PipeWriter page, ValueTask<FlushResult> current, ValueTask<T> task, Template<T> handler, CancellationToken token)
    {
        var flushResult = await current.ConfigureAwait(false);
        if (flushResult.IsCompleted || flushResult.IsCanceled) return flushResult;
        if (!task.IsCompleted)
        {
            flushResult = await page.FlushAsync(token);
        }
        if (flushResult.IsCompleted || flushResult.IsCanceled) return flushResult;
        var prop = await task;
        return await handler((page, token), prop);
    }

    private static async ValueTask<FlushResult> Handle<T>(PipeWriter page, ValueTask<FlushResult> current, IAsyncEnumerable<T> enumerable, Template<T> template, CancellationToken token)
    {
        var flushResult = await current.ConfigureAwait(false);
        if (flushResult.IsCompleted || flushResult.IsCanceled) return flushResult;
        await using var enumerator = enumerable.GetAsyncEnumerator(token);
        while (true)
        {
            var next = enumerator.MoveNextAsync();
            if (!next.IsCompleted)
            {
                flushResult = await page.FlushAsync(token);
                if (flushResult.IsCompleted || flushResult.IsCanceled) return flushResult;
            }
            if (!await next) return flushResult;
            flushResult = await template((page, token), enumerator.Current);
            if (flushResult.IsCompleted || flushResult.IsCanceled) return flushResult;
        }
    }

    private static async ValueTask<FlushResult> Handle<T>(PipeWriter page, ValueTask<FlushResult> current, IEnumerable<T> enumerable, Template<T> template, CancellationToken token)
    {
        var flushResult = await current.ConfigureAwait(false);
        if (flushResult.IsCompleted || flushResult.IsCanceled) return flushResult;
        using var enumerator = enumerable.GetEnumerator();
        while (enumerator.MoveNext())
        {
            flushResult = await template((page, token), enumerator.Current);
            if (flushResult.IsCompleted || flushResult.IsCanceled) return flushResult;
        }
        return flushResult;
    }
}
