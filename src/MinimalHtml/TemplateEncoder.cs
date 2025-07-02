using System.Buffers;
using System.Collections.Frozen;
using System.Text.Unicode;

namespace MinimalHtml;

public class TemplateEncoder
{
    private const int MaxStackAlloc = 256;

    public static readonly TemplateEncoder Html = new([
        '<',
        '>',
        '"',
        '\'',
        '&',
        ], [
        (byte)'<',
        (byte)'>',
        (byte)'"',
        (byte)'\'',
        (byte)'&',
        ], [
        "&lt;"u8.ToArray(),
        "&gt;"u8.ToArray(),
        "&quot;"u8.ToArray(),
        "&#39;"u8.ToArray(),
        "&amp;"u8.ToArray(),
        ]);

    private readonly SearchValues<char> _chars;
    private readonly SearchValues<byte> _bytes;
    private readonly FrozenDictionary<char, byte[]> _charDict;
    private readonly FrozenDictionary<byte, byte[]> _byteDict;

    public TemplateEncoder(ReadOnlySpan<char> chars, ReadOnlySpan<byte> bytes, ReadOnlySpan<byte[]> escaped)
    {
        _chars = SearchValues.Create(chars);
        _bytes = SearchValues.Create(bytes);
        _charDict = ToDict(chars, escaped).ToFrozenDictionary();
        _byteDict = ToDict(bytes, escaped).ToFrozenDictionary();
    }

    static IEnumerable<KeyValuePair<A, B>> ToDict<A, B>(ReadOnlySpan<A> a, ReadOnlySpan<B> b)
    {
        var i = 0;
        foreach (var item in a)
        {
            yield return new KeyValuePair<A, B>(item, b[i]);
            i++;
        }
    }

    public void WriteEncoded(IBufferWriter<byte> writer, ReadOnlySpan<char> input)
    {
        foreach (var range in input.SplitAny(_chars))
        {
            var bytes = input[range];
            WriteUnescaped(writer, bytes);
            if (input.Length > range.End.Value)
            {
                var toEncode = input[range.End.Value];
                var encoded = _charDict[toEncode];
                writer.Write(encoded);
            }
        }
    }

    public void WriteEncoded(IBufferWriter<byte> writer, ReadOnlySpan<byte> input)
    {
        foreach (var range in input.SplitAny(_bytes))
        {
            var bytes = input[range];
            writer.Write(bytes);
            if (input.Length > range.End.Value)
            {
                var toEncode = input[range.End.Value];
                var encoded = _byteDict[toEncode];
                writer.Write(encoded);
            }
        }
    }

    public void WriteEncoded<T>(IBufferWriter<byte> writer, T t, ReadOnlySpan<char> format, IFormatProvider? provider) where T : IUtf8SpanFormattable
    {
        var span = writer.GetSpan();
        int bytesWritten;

        while (!t.TryFormat(span, out bytesWritten, format, provider))
        {
            Increase(writer, ref span);
        }

        var formatted = span.Slice(0, bytesWritten);

        var charIndex = formatted.IndexOfAny(_bytes);

        if (charIndex == -1)
        {
            writer.Advance(bytesWritten);
            return;
        }

        Span<byte> encodingBuffer = stackalloc byte[8];
        byte[]? rented = null;
        scoped Span<byte> source = [];

        if (bytesWritten <= MaxStackAlloc)
        {
            source = stackalloc byte[bytesWritten];
        }
        else
        {
            rented = ArrayPool<byte>.Shared.Rent(bytesWritten);
            source = rented;
        }

        try
        {
            formatted.CopyTo(source);
            WriteEncoded(writer, source);
        }
        finally
        {
            if (rented != null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    private static void WriteUnescaped(IBufferWriter<byte> writer, ReadOnlySpan<char> input)
    {
        var span = writer.GetSpan();
        int bytesWritten;
        while (Utf8.FromUtf16(input, span, out var charsRead, out bytesWritten) == OperationStatus.DestinationTooSmall)
        {
            input = input.Slice(charsRead);
            writer.Advance(bytesWritten);
            span = writer.GetSpan();
        }
        writer.Advance(bytesWritten);
    }

    private static void Increase(IBufferWriter<byte> writer, ref Span<byte> span)
    {
        const int MaxBufferSize = int.MaxValue / 2;
        if (span.Length >= MaxBufferSize) throw new InsufficientMemoryException();
        span = writer.GetSpan(span.Length << 1);
    }
}
