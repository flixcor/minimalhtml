using System.Buffers;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO.Hashing;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Internal;

namespace MinimalForms;

internal static class UrlEncodedFormDictionaryExtensions
{
    private const int StackAllocThreshold = 128;
    private const int DefaultValueCountLimit = 1024;
    private const int KeyLengthLimit = 1024 * 2;
    private const int ValueLengthLimit = 1024 * 1024 * 4;


    public static async Task<FormDictionary> GetUrlEncodedFormDictionaryAsync(this PipeReader reader, CancellationToken token)
    {
        var movingIndex = 0;
        var result = new Dictionary<ulong, Values<Range>>();
        var outputBuffer = ArrayPool<byte>.Shared.Rent(8096);

        try
        {
            while (true)
            {
                var readResult = await reader.ReadAsync(token);
                var buffer = readResult.Buffer;
                if (!buffer.IsEmpty)
                {
                    try
                    {
                        Process(ref buffer, readResult.IsCompleted, result, ref outputBuffer, ref movingIndex);
                    }
                    catch
                    {
                        reader.AdvanceTo(buffer.Start, buffer.End);
                        throw;
                    }
                }

                if (readResult.IsCompleted)
                {
                    reader.AdvanceTo(buffer.End);

                    if (!buffer.IsEmpty)
                    {
                        throw new InvalidOperationException("End of body before form was fully parsed.");
                    }
                    break;
                }

                reader.AdvanceTo(buffer.Start, buffer.End);
            }
            return new(outputBuffer, result.ToFrozenDictionary(), FrozenDictionary<ulong, Values<FormFileInternal>>.Empty);
        }
        catch
        {
            ArrayPool<byte>.Shared.Return(outputBuffer);
            throw;
        }

    }

    private static void Process(ref ReadOnlySequence<byte> buffer, bool isFinalBlock, Dictionary<ulong, Values<Range>> dictionary, ref byte[] outputBuffer, ref int movingIndex)
    {
        if (buffer.IsSingleSegment)
        {
            ProcessFast(buffer.FirstSpan,
                isFinalBlock,
                out var consumed,
                dictionary,
                ref outputBuffer,
                ref movingIndex);

            buffer = buffer.Slice(consumed);
            return;
        }

        ProcessSlow(ref buffer,
            isFinalBlock,
            dictionary,
            ref outputBuffer,
            ref movingIndex);
    }

    private static void ProcessSlow(ref ReadOnlySequence<byte> buffer, bool isFinalBlock, Dictionary<ulong, Values<Range>> dictionary, ref byte[] outputBuffer, ref int movingIndex)
    {
        var sequenceReader = new SequenceReader<byte>(buffer);
        ReadOnlySequence<byte> keyValuePair;

        var consumed = sequenceReader.Position;
        var consumedBytes = default(long);
        var equalsDelimiter = "="u8;
        var andDelimiter = "&"u8;

        while (!sequenceReader.End)
        {
            if (!sequenceReader.TryReadTo(out keyValuePair, andDelimiter))
            {
                if (!isFinalBlock)
                {
                    // +2 to account for '&' and '='
                    if (sequenceReader.Length - consumedBytes > KeyLengthLimit + (long)ValueLengthLimit + 2)
                    {
                        throw new Exception();
                    }
                    break;
                }

                // This must be the final key=value pair
                keyValuePair = buffer.Slice(sequenceReader.Position);
                sequenceReader.Advance(keyValuePair.Length);
            }

            if (keyValuePair.IsSingleSegment)
            {
                ProcessFast(keyValuePair.FirstSpan, isFinalBlock: true, out var segmentConsumed, dictionary, ref outputBuffer, ref movingIndex);
                Debug.Assert(segmentConsumed == keyValuePair.FirstSpan.Length);
                consumedBytes = sequenceReader.Consumed;
                consumed = sequenceReader.Position;
                continue;
            }

            var keyValueReader = new SequenceReader<byte>(keyValuePair);
            ReadOnlySequence<byte> value;
            if (keyValueReader.TryReadTo(out ReadOnlySequence<byte> key, equalsDelimiter))
            {
                if (key.Length > KeyLengthLimit)
                {
                    throw new Exception();
                }

                value = keyValuePair.Slice(keyValueReader.Position);
                if (value.Length > ValueLengthLimit)
                {
                    throw new Exception();
                }
            }
            else
            {
                // Too long for the whole segment to be a key.
                if (keyValuePair.Length > KeyLengthLimit)
                {
                    throw new Exception();
                }

                // There is no more data, this segment must be "key" with no equals or value.
                key = keyValuePair;
                value = default;
            }

            if (!key.IsEmpty && !value.IsEmpty)
            {
                var hash = GetHash(key);
                var length = WriteDecoded(value, ref outputBuffer, movingIndex);
                ref var values = ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary, hash, out _);
                values = values.Add(new(movingIndex, movingIndex += length));
            }

            consumedBytes = sequenceReader.Consumed;
            consumed = sequenceReader.Position;
        }

        buffer = buffer.Slice(consumed);
    }

    // Fast parsing for single span in ReadOnlySequence
    private static void ProcessFast(ReadOnlySpan<byte> span,
        bool isFinalBlock, out int consumed, Dictionary<ulong, Values<Range>> dictionary, ref byte[] outputBuffer, ref int movingIndex
        )
    {
        ReadOnlySpan<byte> key;
        ReadOnlySpan<byte> value;
        consumed = 0;
        var equalsDelimiter = "="u8;
        var andDelimiter = "&"u8;

        while (span.Length > 0)
        {
            // Find the end of the key=value pair.
            var ampersand = span.IndexOf(andDelimiter);
            ReadOnlySpan<byte> keyValuePair;
            int equals;
            var foundAmpersand = ampersand != -1;

            if (foundAmpersand)
            {
                keyValuePair = span.Slice(0, ampersand);
                span = span.Slice(keyValuePair.Length + andDelimiter.Length);
                consumed += keyValuePair.Length + andDelimiter.Length;
            }
            else
            {
                // We can't know that what is currently read is the end of the form value, that's only the case if this is the final block
                // If we're not in the final block, then consume nothing
                if (!isFinalBlock)
                {
                    // Don't buffer indefinitely
                    if ((uint)span.Length > KeyLengthLimit + (uint)ValueLengthLimit)
                    {
                        throw new Exception();
                    }
                    return;
                }

                keyValuePair = span;
                span = default;
                consumed += keyValuePair.Length;
            }

            equals = keyValuePair.IndexOf(equalsDelimiter);

            if (equals == -1)
            {
                // Too long for the whole segment to be a key.
                if (keyValuePair.Length > KeyLengthLimit)
                {
                    throw new Exception();
                }

                // There is no more data, this segment must be "key" with no equals or value.
                key = keyValuePair;
                value = default;
            }
            else
            {
                key = keyValuePair.Slice(0, equals);
                if (key.Length > KeyLengthLimit)
                {
                    throw new Exception();
                }

                value = keyValuePair.Slice(equals + equalsDelimiter.Length);
                if (value.Length > ValueLengthLimit)
                {
                    throw new Exception();
                }
            }

            if (!key.IsEmpty && !value.IsEmpty)
            {
                key = Decode(key);
                value = Decode(value);

                var hash = XxHash3.HashToUInt64(key);
                ref var values = ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary, hash, out _);
                Write(value, ref outputBuffer, movingIndex);
                Range range = new(movingIndex, movingIndex += value.Length);
                values = values.Add(range);
            }
        }
    }

    private static void Write(ReadOnlySpan<byte> bytes, ref byte[] outputBuffer, int movingIndex)
    {
        Grow(bytes.Length, ref outputBuffer, movingIndex);
        bytes.CopyTo(outputBuffer.AsSpan(movingIndex));
    }

    private static void Grow(int length, ref byte[] outputBuffer, int movingIndex)
    {
        if (movingIndex + length > outputBuffer.Length)
        {
            var newBuffer = ArrayPool<byte>.Shared.Rent(Math.Max(outputBuffer.Length * 2, movingIndex + length));
            outputBuffer.AsSpan(0, movingIndex).CopyTo(newBuffer);
            ArrayPool<byte>.Shared.Return(outputBuffer);
            outputBuffer = newBuffer;
        }
    }

    private static int WriteDecoded(ReadOnlySequence<byte> ros, ref byte[] outputBuffer, int movingIndex)
    {
        if (ros.IsSingleSegment)
        {
            var span = Decode(ros.FirstSpan);
            Write(span, ref outputBuffer, movingIndex);
            return span.Length;
        }

        Grow((int)ros.Length, ref outputBuffer, movingIndex);

        var partToCopyTo = outputBuffer.AsSpan(movingIndex);
        ros.CopyTo(partToCopyTo);
        var decoded = Decode(partToCopyTo);
        return decoded.Length;
    }

    private static ReadOnlySpan<byte> Decode(ReadOnlySpan<byte> readOnlySpan)
    {
        var span = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(readOnlySpan), readOnlySpan.Length);
        return Decode(span);
    }

    private static ReadOnlySpan<byte> Decode(Span<byte> span)
    {
        try
        {
            var bytes = UrlDecoder.DecodeInPlace(span, isFormEncoding: true);
            var result = span.Slice(0, bytes);
            return result;

        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidDataException("The form value contains invalid characters.", ex);
        }
    }

    private static ulong GetHash(ReadOnlySequence<byte> key)
    {
        if (key.Length <= StackAllocThreshold)
        {
            Span<byte> buffer = stackalloc byte[(int)key.Length];
            key.CopyTo(buffer);

            return XxHash3.HashToUInt64(Decode(buffer));
        }

        var rented = ArrayPool<byte>.Shared.Rent((int)key.Length);
        try
        {
            return XxHash3.HashToUInt64(Decode(rented));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    public static Dictionary<ulong, Values<Range>> GetFormDictionary(this ReadOnlySpan<byte> buffer)
    {
        var and = "&"u8;
        var equals = "="u8;
        var dict = new Dictionary<ulong, Values<Range>>();

        foreach (var range in buffer.Split(and))
        {
            var item = buffer[range];
            var equalsIndex = item.IndexOf(equals);
            if (equalsIndex == -1) continue;
            var hash = XxHash3.HashToUInt64(item.Slice(0, equalsIndex));
            var innerRange = new Range(range.Start, new(range.Start.Value + equalsIndex + 1));
            ref var values = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, hash, out _);
            values = values.Add(innerRange);
        }

        return dict;
    }
}
