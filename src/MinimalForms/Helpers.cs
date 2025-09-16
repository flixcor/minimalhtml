using System.Buffers;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace MinimalForms
{
    public static class Helpers
    {
        private const int MaxBufferSize = int.MaxValue / 2;
        private const int MaxStackAlloc = 256;

        private static readonly SearchValues<char> s_characters = SearchValues.Create(
            '<',
            '>',
            '"',
            '\'',
            '&');

        private static ReadOnlySpan<byte> GetEscapedCharacter(char c) => c switch
            {
                '<' => "&lt;"u8,
                '>' => "&gt;"u8,
                '"' => "&quot;"u8,
                '\'' => "&#39;"u8,
                '&' => "&amp;"u8,
                _ => [],
            };

        public static void WriteHtmlEscaped(this IBufferWriter<byte> writer, ReadOnlySpan<char> input)
        {
            foreach (var range in input.SplitAny(s_characters))
            {
                var chars = input[range];
                writer.WriteUnescaped(chars);
                if (input.Length > range.End.Value)
                {
                    var toEncode = input[range.End.Value];
                    var encoded = GetEscapedCharacter(toEncode);
                    writer.Write(encoded);
                }
            }
        }

        public static bool TryWriteHtmlEscaped(ReadOnlySpan<char> input, Span<byte> output, out int written)
        {
            written = 0;
            foreach (var range in input.SplitAny(s_characters))
            {
                var chars = input[range];
                if(!TryWriteUnescaped(chars, output, out var nextWritten)) return false;
                output = output.Slice(nextWritten);
                written += nextWritten;

                if (input.Length > range.End.Value)
                {
                    var toEncode = GetEscapedCharacter(input[range.End.Value]);
                    if(!toEncode.TryCopyTo(output)) return false;
                    output = output.Slice(toEncode.Length);
                    written += toEncode.Length;
                }
            }
            return true;
        }

        public static void WriteEncoded(this IBufferWriter<byte> writer, ReadOnlySpan<byte> input, TextEncoder encoder)
        {
            while (!input.IsEmpty)
            {
                var span = writer.GetSpan();
                encoder.EncodeUtf8(input, span, out var consumed, out var written, true);
                writer.Advance(written);
                input = input.Slice(consumed);
            }
        }

        public static void WriteEncoded<T>(this IBufferWriter<byte> writer, T t, ReadOnlySpan<char> format, IFormatProvider? provider, TextEncoder encoder) where T : IUtf8SpanFormattable
        {
            var span = writer.GetSpan();
            int bytesWritten;

            while (!t.TryFormat(span, out bytesWritten, format, provider))
            {
                Increase(writer, ref span);
            }

            var formatted = span.Slice(0, bytesWritten);

            var charIndex = encoder.FindFirstCharacterToEncodeUtf8(formatted);

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

                    writer.Next(before, ref span, ref bytesWritten);

                    encoder.EncodeUtf8(byteToEncode, encodingBuffer, out _, out var bytesEncoded);

                    writer.Next(encodingBuffer.Slice(0, bytesEncoded), ref span, ref bytesWritten);

                    charIndex = encoder.FindFirstCharacterToEncodeUtf8(source);
                } while (charIndex > -1);

                writer.Next(source, ref span, ref bytesWritten);
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

        public static bool TryWriteEscaped(ReadOnlySpan<byte> input, Span<byte> utf8Destination, out int bytesWritten) =>
            HtmlEncoder.Default.EncodeUtf8(input, utf8Destination, out _, out bytesWritten) == OperationStatus.Done;

        public static void WriteFormatted<T>(T t, IBufferWriter<byte> pipeWriter, string? format = null, IFormatProvider? provider = null) where T : IUtf8SpanFormattable
        {
            var span = pipeWriter.GetSpan();
            int bytesWritten;
            while (!t.TryFormat(span, out bytesWritten, format, provider))
            {
                if (span.Length >= MaxBufferSize) throw new InsufficientMemoryException();
                span = pipeWriter.GetSpan(span.Length << 1);
            }
            pipeWriter.Advance(bytesWritten);
        }

        private static void WriteUnescaped(this IBufferWriter<byte> writer, ReadOnlySpan<char> input)
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

        private static bool TryWriteUnescaped(ReadOnlySpan<char> input, Span<byte> output, out int bytesWritten)
            => Utf8.FromUtf16(input, output, out _, out bytesWritten) == OperationStatus.Done;

        private static void Next(this IBufferWriter<byte> writer, scoped ReadOnlySpan<byte> input, ref Span<byte> current, ref int bytesWritten)
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

        private static void Increase(this IBufferWriter<byte> writer, ref Span<byte> span)
        {
            const int MaxBufferSize = int.MaxValue / 2;
            if (span.Length >= MaxBufferSize) throw new InsufficientMemoryException();
            span = writer.GetSpan(span.Length << 1);
        }
    }
}
