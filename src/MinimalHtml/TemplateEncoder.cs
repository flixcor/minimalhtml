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
        if(chars.Length != bytes.Length || chars.Length != escaped.Length)
            throw new ArgumentException("All input spans must have the same length.");
        _chars = SearchValues.Create(chars);
        _bytes = SearchValues.Create(bytes);
        _charDict = ToDict(chars, escaped).ToFrozenDictionary();
        _byteDict = ToDict(bytes, escaped).ToFrozenDictionary();
    }

    static Dictionary<A, B> ToDict<A, B>(ReadOnlySpan<A> a, ReadOnlySpan<B> b) where A : notnull
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Input spans must have the same length.");
        var dict = new Dictionary<A, B>(a.Length);
        for (var i = 0; i < a.Length; i++)
        {
            dict[a[i]] = b[i];
        }
        return dict;
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

        byte[]? rented = null;
        scoped Span<byte> copy = [];

        if (bytesWritten <= MaxStackAlloc)
        {
            copy = stackalloc byte[bytesWritten];
        }
        else
        {
            rented = ArrayPool<byte>.Shared.Rent(bytesWritten);
            copy = rented;
        }

        try
        {
            formatted.CopyTo(copy);
            WriteEncoded(writer, copy);
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
