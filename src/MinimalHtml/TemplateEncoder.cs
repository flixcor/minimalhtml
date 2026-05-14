using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text.Unicode;

namespace MinimalHtml;

/// <summary>
/// A class for encoding HTML content.
/// </summary>
public class TemplateEncoder
{
    private const int MaxStackAlloc = 256;

    /// <summary>
    /// A static instance of the <see cref="TemplateEncoder"/> class that encodes html
    /// </summary>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateEncoder"/> class with the specified characters, their corresponding byte representations, and their escaped byte representations. The constructor creates lookup tables for efficient encoding of characters and bytes when writing HTML content. The input spans must have the same length, as they correspond to each other in terms of characters, bytes, and their escaped forms.
    /// </summary>
    /// <param name="chars"></param>
    /// <param name="bytes"></param>
    /// <param name="escaped"></param>
    /// <exception cref="ArgumentException"></exception>
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

    /// <summary>
    /// Writes the specified input to the provided bufferwriter, encoding any characters that require escaping according to the lookup tables initialized in the constructor. The method first checks for the presence of any characters that need to be escaped using the _chars search values. If no such characters are found, it writes the input directly to the buffer. If characters that require escaping are found, it calls a helper method to write the encoded output, ensuring that all necessary characters are properly escaped in the resulting HTML content.
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="input"></param>
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

    /// <summary>
    /// Writes the specified input to the provided bufferwriter, encoding any bytes that require escaping according to the lookup tables initialized in the constructor. The method first checks for the presence of any bytes that need to be escaped using the _bytes search values. If no such bytes are found, it writes the input directly to the buffer. If bytes that require escaping are found, it calls a helper method to write the encoded output, ensuring that all necessary bytes are properly escaped in the resulting HTML content.
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="input"></param>
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

    /// <summary>
    /// Writes the specified value of type T to the provided bufferwriter, encoding any characters that require escaping according to the lookup tables initialized in the constructor. The method formats the value of type T into a byte span, checks for any characters that need to be escaped, and writes the encoded output to the buffer. This allows for efficient encoding of formatted values while ensuring that all necessary characters are properly escaped in the resulting HTML content.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="writer"></param>
    /// <param name="t"></param>
    /// <param name="format"></param>
    /// <param name="provider"></param>
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
