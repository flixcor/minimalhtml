global using Flushed = System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult>;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;

namespace MinimalHtml
{

    public record HtmlWriter(PipeWriter Writer, CancellationToken Token);

    public delegate Flushed Template<in T>(HtmlWriter page, T context);
    public delegate Flushed Template(HtmlWriter page);

    public static class HtmlTemplateExtensions
    {
        private static readonly ConcurrentDictionary<string, byte[]> s_buffers = new();

        public static Flushed Html(
            this HtmlWriter page,
            [InterpolatedStringHandlerArgument(nameof(page))]
            [StringSyntax("Html")]
            ref Handler handler
            ) =>
            handler.Result;

        public static Flushed Html(
            this HtmlWriter page,
            IFormatProvider? provider,
            [InterpolatedStringHandlerArgument(nameof(page), nameof(provider))]
            [StringSyntax("Html")]
            ref Handler handler
            ) =>
            handler.Result;

        public static Flushed Html(
            this HtmlWriter page,
            IFormatProvider? provider,
            TextEncoder? textEncoder,
            [InterpolatedStringHandlerArgument(nameof(page), nameof(provider), nameof(textEncoder))]
            [StringSyntax("Html")]
            ref Handler handler
            ) =>
            handler.Result;

        public static void Precompile(string key, byte[] handler) => s_buffers[key] = handler;

        private static Flushed Flush(this HtmlWriter page) => page.Writer.FlushAsync(page.Token);

        [InterpolatedStringHandler]
        public ref struct Handler
        {
            private readonly HtmlWriter _page;
            private readonly IFormatProvider? _formatProvider;
            private readonly TextEncoder _encoder;

            internal Flushed Result { get; private set; } = new();

            public Handler(int literalLength, int formattedCount, HtmlWriter page) : this(literalLength, formattedCount, page, null, null)
            {
            }

            public Handler(int literalLength, int formattedCount, HtmlWriter page, IFormatProvider? formatProvider) : this(literalLength, formattedCount, page, formatProvider, null)
            {
            }

            public Handler(int literalLength, int formattedCount, HtmlWriter page, IFormatProvider? formatProvider, TextEncoder? encoder)
            {
                _page = page;
                _formatProvider = formatProvider;
                _encoder = encoder ?? HtmlEncoder.Default;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AppendLiteral(string? s)
            {
                if (!string.IsNullOrWhiteSpace(s) && !_page.Token.IsCancellationRequested)
                {
                    Result = Handle(_page, Result, s, static (p, s) => p.Writer.Write(s_buffers.GetOrAdd(s, Encoding.UTF8.GetBytes)));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AppendFormatted(string? s)
            {
                if (!string.IsNullOrEmpty(s))
                {
                    Result = Handle(_page, Result, s, static (p, s) => p.Writer.WriteHtmlEscaped(s));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AppendFormatted(Memory<byte> bytes)
            {
                if (!bytes.IsEmpty)
                {
                    Result = Handle(_page, Result, (bytes, _encoder), static (p, tup) => p.Writer.WriteEncoded(tup.bytes.Span, tup._encoder));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AppendFormatted(Func<ReadOnlySpan<byte>> getBytes)
            {
                Result = Handle(_page, Result, (getBytes, _encoder), static (p, tup) => p.Writer.WriteEncoded(tup.getBytes(), tup._encoder));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AppendFormatted<TFormattable>(TFormattable? t, string? format = null) where TFormattable : IUtf8SpanFormattable
            {
                if (t != null)
                {
                    Result = Handle(_page, Result, (t, format, _formatProvider, _encoder), static (p, tup) => p.Writer.WriteEncoded(tup.t, tup.format, tup._formatProvider, tup._encoder));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AppendFormatted(Template? template)
            {
                if (template is not null)
                {
                    Result = Handle(_page, Result, template);
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
                Result = Handle(_page, Result, tuple.T, tuple.Template);
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
                Result = Handle(_page, Result, tuple.Item1, tuple.Item2);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AppendFormatted<T>((Template<T>, IAsyncEnumerable<T>) tuple)
                => AppendFormatted((tuple.Item2, tuple.Item1));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AppendFormatted<T>((IAsyncEnumerable<T>, Template<T>) tuple)
            {
                Result = Handle(_page, Result, tuple.Item1, tuple.Item2);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AppendFormatted<T>((Template<T>, IEnumerable<T>) tuple)
                => AppendFormatted((tuple.Item2, tuple.Item1));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AppendFormatted<T>((IEnumerable<T>, Template<T>) tuple)
            {
                Result = Handle(_page, Result, tuple.Item1, tuple.Item2);
            }

            private static async Flushed Handle<T>(HtmlWriter page, Flushed current, T state, Action<HtmlWriter, T> handler)
            {
                var flushResult = await current.ConfigureAwait(false);
                if (flushResult.IsCompleted || flushResult.IsCanceled) return flushResult;
                handler(page, state);
                return new();
            }

            private static async Flushed Handle(HtmlWriter page, Flushed current, Template handler)
            {
                var flushResult = await current.ConfigureAwait(false);
                if (flushResult.IsCompleted || flushResult.IsCanceled) return flushResult;
                return await handler(page);
            }

            private static async Flushed Handle<T>(HtmlWriter page, Flushed current, T state, Template<T> handler)
            {
                var flushResult = await current.ConfigureAwait(false);
                if (flushResult.IsCompleted || flushResult.IsCanceled) return flushResult;
                return await handler(page, state);
            }

            private static async Flushed Handle<T>(HtmlWriter page, Flushed current, ValueTask<T> task, Template<T> handler)
            {
                var flushResult = await current.ConfigureAwait(false);
                if (flushResult.IsCompleted || flushResult.IsCanceled) return flushResult;
                if (!task.IsCompleted)
                {
                    flushResult = await page.Flush();
                }
                if (flushResult.IsCompleted || flushResult.IsCanceled) return flushResult;
                var prop = await task;
                return await handler(page, prop);
            }

            private static async Flushed Handle<T>(HtmlWriter page, Flushed current, IAsyncEnumerable<T> enumerable, Template<T> template)
            {
                var flushResult = await current.ConfigureAwait(false);
                if (flushResult.IsCompleted || flushResult.IsCanceled) return flushResult;
                var next = new ValueTask<bool>(false);
                var enumerator = enumerable.GetAsyncEnumerator(page.Token);
                try
                {
                    while (true)
                    {
                        next = enumerator.MoveNextAsync();
                        if (!next.IsCompleted)
                        {
                            flushResult = await page.Flush();
                            if (flushResult.IsCompleted || flushResult.IsCanceled) return flushResult;
                        }
                        if (!await next) return flushResult;
                        flushResult = await template(page, enumerator.Current);
                        if (flushResult.IsCompleted || flushResult.IsCanceled) return flushResult;
                    }
                }
                finally
                {
                    await next;
                    await enumerator.DisposeAsync();
                }
            }

            private static async Flushed Handle<T>(HtmlWriter page, Flushed current, IEnumerable<T> enumerable, Template<T> template)
            {
                var flushResult = await current.ConfigureAwait(false);
                if (flushResult.IsCompleted || flushResult.IsCanceled) return flushResult;
                using var enumerator = enumerable.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    flushResult = await template(page, enumerator.Current);
                    if (flushResult.IsCompleted || flushResult.IsCanceled) return flushResult;
                }
                return flushResult;
            }
        }
    }
}
