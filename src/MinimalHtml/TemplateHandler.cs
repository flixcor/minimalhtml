using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace MinimalHtml;

public interface ITemplateHandler
{
    void AppendFormatted(Func<ReadOnlySpan<byte>> getBytes);
    void AppendFormatted(ReadOnlyMemory<byte> bytes);
    void AppendFormatted(string? s);
    void AppendFormatted(Template? innerTemplate);
    void AppendFormatted(Template<string> innerTemplate, string format);
    void AppendFormatted<T>((IAsyncEnumerable<T>, Template<T>) tuple);
    void AppendFormatted<T>((IEnumerable<T>, Template<T>) tuple);
    void AppendFormatted<T>((T T, Template<T> Template) tuple);
    void AppendFormatted<T>((Task<T> Task, Template<T> Template) tuple);
    void AppendFormatted<T>((Template<T> Template, Task<T> Task) tuple);
    void AppendFormatted<T>((Func<Task<T>> Task, Template<T> Template) tuple);
    void AppendFormatted<T>((Template<T> Template, Func<Task<T>> Task) tuple);
    void AppendFormatted<T>((Template<T>, IAsyncEnumerable<T>) tuple);
    void AppendFormatted<T>((Template<T>, IEnumerable<T>) tuple);
    void AppendFormatted<T>((Template<T>, T) tuple);
    void AppendFormatted<T>((Template<T>, ValueTask<T>) tuple);
    void AppendFormatted<T>((ValueTask<T> Task, Template<T> Template) tuple);
    void AppendFormatted<T>((Template<T> Template, Func<ValueTask<T>> Task) tuple);
    void AppendFormatted<T>((Func<ValueTask<T>> Task, Template<T> Template) tuple);
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

    // ── Sync operations: inline the write directly, no Handle/delegate ──

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLiteral(string? s)
    {
        if (!string.IsNullOrWhiteSpace(s) && !_token.IsCancellationRequested)
        {
#pragma warning disable CA2012
            if (Result.IsCompletedSuccessfully)
                _writer.Write(s_buffers.GetOrAdd(s, Encoding.UTF8.GetBytes));
            else
                Result = AwaitThenWriteLiteral(Result, _writer, s);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(string? s)
    {
        if (!string.IsNullOrEmpty(s) && !_token.IsCancellationRequested)
        {
            if (Result.IsCompletedSuccessfully)
                _encoder.WriteEncoded(_writer, s);
            else
                Result = AwaitThenWriteEncoded(Result, _writer, _encoder, s);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(ReadOnlyMemory<byte> bytes)
    {
        if (!bytes.IsEmpty && !_token.IsCancellationRequested)
        {
            if (Result.IsCompletedSuccessfully)
                _encoder.WriteEncoded(_writer, bytes.Span);
            else
                Result = AwaitThenWriteEncodedBytes(Result, _writer, _encoder, bytes);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(Func<ReadOnlySpan<byte>> getBytes)
    {
        if (!_token.IsCancellationRequested)
        {
            if (Result.IsCompletedSuccessfully)
                _encoder.WriteEncoded(_writer, getBytes());
            else
                Result = AwaitThenWriteEncodedFunc(Result, _writer, _encoder, getBytes);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<TFormattable>(TFormattable? t, string? format = null) where TFormattable : IUtf8SpanFormattable
    {
        if (t != null && !_token.IsCancellationRequested)
        {
            if (Result.IsCompletedSuccessfully)
                _encoder.WriteEncoded(_writer, t, format, _formatProvider);
            else
                Result = AwaitThenWriteFormatted(Result, _writer, _encoder, t, format, _formatProvider);
        }
    }

    // ── Async operations: inline the IsCompletedSuccessfully check, call HandleAsync directly ──

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(Template? innerTemplate)
    {
        if (innerTemplate is not null && !_token.IsCancellationRequested)
        {
            if (Result.IsCompletedSuccessfully)
                Result = innerTemplate((_writer, _token));
            else
                Result = HandleAsync(_writer, Result, innerTemplate, _token);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted(Template<string> innerTemplate, string format)
        => AppendFormatted((format, innerTemplate));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>((Template<T>, T) tuple)
        => AppendFormatted((tuple.Item2, tuple.Item1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>((T T, Template<T> Template) tuple)
    {
        if (Result.IsCompletedSuccessfully)
            Result = tuple.Template((_writer, _token), tuple.T);
        else
            Result = HandleAsync(_writer, Result, tuple.T, tuple.Template, _token);
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
        if (Result.IsCompletedSuccessfully && tuple.Task.IsCompletedSuccessfully)
            Result = tuple.Template((_writer, _token), tuple.Task.Result);
        else
            Result = HandleAsync(_writer, Result, tuple.Task, tuple.Template, _token);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>((Template<T>, IAsyncEnumerable<T>) tuple)
        => AppendFormatted((tuple.Item2, tuple.Item1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>((IAsyncEnumerable<T>, Template<T>) tuple)
    {
        Result = HandleAsync(_writer, Result, tuple.Item1, tuple.Item2, _token);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>((Template<T>, IEnumerable<T>) tuple)
        => AppendFormatted((tuple.Item2, tuple.Item1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>((IEnumerable<T>, Template<T>) tuple)
    {
        Result = HandleAsync(_writer, Result, tuple.Item1, tuple.Item2, _token);
    }

    public void AppendFormatted<T>((Func<Task<T>> Task, Template<T> Template) tuple) => AppendFormatted((tuple.Task(), tuple.Template));

    public void AppendFormatted<T>((Template<T> Template, Func<Task<T>> Task) tuple) => AppendFormatted((tuple.Task(), tuple.Template));

    public void AppendFormatted<T>((Template<T> Template, Func<ValueTask<T>> Task) tuple) => AppendFormatted((tuple.Task(), tuple.Template));

    public void AppendFormatted<T>((Func<ValueTask<T>> Task, Template<T> Template) tuple) => AppendFormatted((tuple.Task(), tuple.Template));

    // ── Static async helpers for sync-after-async transitions (no boxing, no closures) ──

    private static async ValueTask<FlushResult> AwaitThenWriteLiteral(ValueTask<FlushResult> pending, PipeWriter writer, string s)
    {
        var r = await pending.ConfigureAwait(false);
        if (r.IsCompleted || r.IsCanceled) return r;
        writer.Write(s_buffers.GetOrAdd(s, Encoding.UTF8.GetBytes));
        return new();
    }

    private static async ValueTask<FlushResult> AwaitThenWriteEncoded(ValueTask<FlushResult> pending, PipeWriter writer, TemplateEncoder encoder, string s)
    {
        var r = await pending.ConfigureAwait(false);
        if (r.IsCompleted || r.IsCanceled) return r;
        encoder.WriteEncoded(writer, s);
        return new();
    }

    private static async ValueTask<FlushResult> AwaitThenWriteEncodedBytes(ValueTask<FlushResult> pending, PipeWriter writer, TemplateEncoder encoder, ReadOnlyMemory<byte> bytes)
    {
        var r = await pending.ConfigureAwait(false);
        if (r.IsCompleted || r.IsCanceled) return r;
        encoder.WriteEncoded(writer, bytes.Span);
        return new();
    }

    private static async ValueTask<FlushResult> AwaitThenWriteEncodedFunc(ValueTask<FlushResult> pending, PipeWriter writer, TemplateEncoder encoder, Func<ReadOnlySpan<byte>> getBytes)
    {
        var r = await pending.ConfigureAwait(false);
        if (r.IsCompleted || r.IsCanceled) return r;
        encoder.WriteEncoded(writer, getBytes());
        return new();
    }

    private static async ValueTask<FlushResult> AwaitThenWriteFormatted<TFormattable>(ValueTask<FlushResult> pending, PipeWriter writer, TemplateEncoder encoder, TFormattable t, string? format, IFormatProvider? provider) where TFormattable : IUtf8SpanFormattable
    {
        var r = await pending.ConfigureAwait(false);
        if (r.IsCompleted || r.IsCanceled) return r;
        encoder.WriteEncoded(writer, t, format, provider);
        return new();
    }

    // ── Async fallback methods ──

    private static async ValueTask<FlushResult> HandleAsync(PipeWriter page, ValueTask<FlushResult> current, Template handler, CancellationToken token)
    {
        var flushResult = await current.ConfigureAwait(false);
        if (flushResult.IsCompleted || flushResult.IsCanceled) return flushResult;
        return await handler((page, token)).ConfigureAwait(false);
    }

    private static async ValueTask<FlushResult> HandleAsync<T>(PipeWriter page, ValueTask<FlushResult> current, T state, Template<T> handler, CancellationToken token)
    {
        var flushResult = await current.ConfigureAwait(false);
        if (flushResult.IsCompleted || flushResult.IsCanceled) return flushResult;
        return await handler((page, token), state).ConfigureAwait(false);
    }

    private static async ValueTask<FlushResult> HandleAsync<T>(PipeWriter page, ValueTask<FlushResult> current, ValueTask<T> task, Template<T> handler, CancellationToken token)
    {
        var flushResult = await current.ConfigureAwait(false);
        if (flushResult.IsCompleted || flushResult.IsCanceled) return flushResult;
        if (!task.IsCompleted)
        {
            flushResult = await page.FlushAsync(token).ConfigureAwait(false);
        }
        if (flushResult.IsCompleted || flushResult.IsCanceled) return flushResult;
        var prop = await task.ConfigureAwait(false);
        return await handler((page, token), prop).ConfigureAwait(false);
    }

    private static async ValueTask<FlushResult> HandleAsync<T>(PipeWriter page, ValueTask<FlushResult> current, IAsyncEnumerable<T> enumerable, Template<T> template, CancellationToken token)
    {
        var flushResult = await current.ConfigureAwait(false);
        if (flushResult.IsCompleted || flushResult.IsCanceled) return flushResult;
        var enumerator = enumerable.GetAsyncEnumerator(token);
        await using var _ = enumerator.ConfigureAwait(false);
        while (true)
        {
            var next = enumerator.MoveNextAsync();
            if (!next.IsCompleted)
            {
                flushResult = await page.FlushAsync(token).ConfigureAwait(false);
                if (flushResult.IsCompleted || flushResult.IsCanceled) return flushResult;
            }
            if (!await next.ConfigureAwait(false)) return flushResult;
            flushResult = await template((page, token), enumerator.Current).ConfigureAwait(false);
            if (flushResult.IsCompleted || flushResult.IsCanceled) return flushResult;
        }
    }

    private static async ValueTask<FlushResult> HandleAsync<T>(PipeWriter page, ValueTask<FlushResult> current, IEnumerable<T> enumerable, Template<T> template, CancellationToken token)
    {
        var flushResult = await current.ConfigureAwait(false);
        if (flushResult.IsCompleted || flushResult.IsCanceled) return flushResult;
        using var enumerator = enumerable.GetEnumerator();
        while (enumerator.MoveNext())
        {
            flushResult = await template((page, token), enumerator.Current).ConfigureAwait(false);
            if (flushResult.IsCompleted || flushResult.IsCanceled) return flushResult;
        }
        return flushResult;
    }
}
#pragma warning restore CA2012
