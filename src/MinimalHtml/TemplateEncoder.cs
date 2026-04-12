using System.Buffers;
using System.Runtime.CompilerServices;
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
    private readonly byte[]?[] _charLookup;
    private readonly byte[]?[] _byteLookup;

    public TemplateEncoder(ReadOnlySpan<char> chars, ReadOnlySpan<byte> bytes, ReadOnlySpan<byte[]> escaped)
    {
        if(chars.Length != bytes.Length || chars.Length != escaped.Length)
            throw new ArgumentException("All input spans must have the same length.");
        _chars = SearchValues.Create(chars);
        _bytes = SearchValues.Create(bytes);
        _charLookup = new byte[]?[128];
        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] < 128)
                _charLookup[chars[i]] = escaped[i];
        }
        _byteLookup = new byte[]?[256];
        for (var i = 0; i < bytes.Length; i++)
        {
            _byteLookup[bytes[i]] = escaped[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteEncoded(IBufferWriter<byte> writer, ReadOnlySpan<char> input)
    {
        var idx = input.IndexOfAny(_chars);
        if (idx < 0)
        {
            WriteUnescaped(writer, input);
            return;
        }
        WriteEncodedSlow(writer, input, idx);
    }

    private void WriteEncodedSlow(IBufferWriter<byte> writer, ReadOnlySpan<char> input, int idx)
    {
        do
        {
            if (idx > 0)
                WriteUnescaped(writer, input.Slice(0, idx));
            writer.Write(_charLookup[input[idx]]!);
            input = input.Slice(idx + 1);
            idx = input.IndexOfAny(_chars);
        } while (idx >= 0);

        if (!input.IsEmpty)
            WriteUnescaped(writer, input);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteEncoded(IBufferWriter<byte> writer, ReadOnlySpan<byte> input)
    {
        var idx = input.IndexOfAny(_bytes);
        if (idx < 0)
        {
            writer.Write(input);
            return;
        }
        WriteEncodedSlow(writer, input, idx);
    }

    private void WriteEncodedSlow(IBufferWriter<byte> writer, ReadOnlySpan<byte> input, int idx)
    {
        do
        {
            if (idx > 0)
                writer.Write(input.Slice(0, idx));
            writer.Write(_byteLookup[input[idx]]!);
            input = input.Slice(idx + 1);
            idx = input.IndexOfAny(_bytes);
        } while (idx >= 0);

        if (!input.IsEmpty)
            writer.Write(input);
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
            WriteEncodedSlow(writer, copy.Slice(0, bytesWritten), charIndex);
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
