using System.Buffers;
using System.Collections.Immutable;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace MinimalHtml;

public class TemplateEncoder
{
    private const int MaxStackAlloc = 256;

    public static readonly TemplateEncoder Html = new(
        HtmlEncoder.Default,
        ImmutableDictionary.Create<char, Func<ReadOnlySpan<byte>>>()
            .Add('<', () => "<"u8)
            .Add('>', () => ">"u8)
            .Add('"', () => "\""u8)
            .Add('\'', () => "'"u8)
            .Add('&', () => "&"u8));

    private readonly TextEncoder _encoder;
    private readonly ImmutableDictionary<char, Func<ReadOnlySpan<byte>>> _dict;
    private readonly SearchValues<char> _searchValues;

    public TemplateEncoder(TextEncoder encoder, ImmutableDictionary<char, Func<ReadOnlySpan<byte>>> dict)
    {
        _encoder = encoder;
        _dict = dict;
        _searchValues = SearchValues.Create(dict.Keys.ToArray());
    }

    public void WriteEncoded(IBufferWriter<byte> writer, ReadOnlySpan<char> input)
    {
        foreach (var range in input.SplitAny(_searchValues))
        {
            var bytes = input[range];
            WriteUnescaped(writer, bytes);
            if (input.Length > range.End.Value)
            {
                var toEncode = input[range.End.Value];
                var getSpan = _dict[toEncode];
                writer.Write(getSpan());
            }
        }
    }

    public void WriteEncoded(IBufferWriter<byte> writer, ReadOnlySpan<byte> input)
    {
        while (!input.IsEmpty)
        {
            var span = writer.GetSpan();
            _encoder.EncodeUtf8(input, span, out var consumed, out var written, true);
            writer.Advance(written);
            input = input.Slice(consumed);
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

        var charIndex = _encoder.FindFirstCharacterToEncodeUtf8(formatted);

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
            bytesWritten = 0;

            do
            {
                var byteToEncode = source.Slice(charIndex, 1);
                var before = source.Slice(0, charIndex);

                Next(writer, before, ref span, ref bytesWritten);

                _encoder.EncodeUtf8(byteToEncode, encodingBuffer, out _, out var bytesEncoded);

                Next(writer, encodingBuffer.Slice(0, bytesEncoded), ref span, ref bytesWritten);

                charIndex = _encoder.FindFirstCharacterToEncodeUtf8(source);
            } while (charIndex > -1);

            Next(writer, source, ref span, ref bytesWritten);
            writer.Advance(bytesWritten);
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

    private static void Next(IBufferWriter<byte> writer, scoped ReadOnlySpan<byte> input, ref Span<byte> current, ref int bytesWritten)
    {
        while (!input.TryCopyTo(current))
        {
            var before = input.Slice(0, current.Length);
            input = input.Slice(current.Length);
            before.CopyTo(current);
            writer.Advance(bytesWritten + current.Length);
            bytesWritten = 0;
            current = writer.GetSpan();
        }
        bytesWritten += input.Length;
        current = current.Slice(0, input.Length);
    }

    private static void Increase(IBufferWriter<byte> writer, ref Span<byte> span)
    {
        const int MaxBufferSize = int.MaxValue / 2;
        if (span.Length >= MaxBufferSize) throw new InsufficientMemoryException();
        span = writer.GetSpan(span.Length << 1);
    }
}
