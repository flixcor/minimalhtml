using System.Buffers;
using System.Collections.Frozen;
using System.IO.Hashing;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using System.Text.Unicode;

namespace MinimalForms;

internal static class MultipartFormDictionaryExtensions
{
    private const int StackAllocThreshold = 128;
    internal static Task<FormDictionary> GetMultipartFormDicationaryAsync(this PipeReader reader, Boundary boundary, long? length, CancellationToken token) =>
        reader.GetMultipartFormDicationaryInternal(ConvertBoundary(boundary), length, token);

    private static async Task<FormDictionary> GetMultipartFormDicationaryInternal(this PipeReader reader, byte[] boundary, long? length, CancellationToken token)
    {
        const int DefaultBufferSize = 8096;
        var movingIndex = 0;
        var result = new Dictionary<ulong, Values<Range>>();
        var files = new Dictionary<ulong, Values<FormFileInternal>>();
        var lengthGuess = length.HasValue 
            ? (int)Math.Min(length.GetValueOrDefault(), DefaultBufferSize) 
            : DefaultBufferSize;
        var outputBuffer = ArrayPool<byte>.Shared.Rent(lengthGuess);
        FileStream? file = null;
        Range contentType = default;
        Range fileName = default;
        ulong keyHash = default;
        var headersDone = true;

        try
        {
            while (!token.IsCancellationRequested)
            {
                var readResult = await reader.ReadAsync(token);
                var buffer = readResult.Buffer;
                if (!buffer.IsEmpty)
                {
                    try
                    {
                        while (Process(
                            ref buffer,
                            readResult.IsCompleted,
                            boundary,
                            result,
                            files,
                            ref outputBuffer,
                            ref movingIndex,
                            ref file,
                            ref keyHash,
                            ref fileName,
                            ref contentType,
                            ref headersDone))
                        {
                            if (file != default)
                            {
                                await file.FlushAsync(token);
                                if (!headersDone)
                                {
                                    await file.DisposeAsync();
                                    file = null;
                                }
                            }
                        }
                    }
                    catch
                    {
                        reader.AdvanceTo(buffer.Start, buffer.End);
                        throw;
                    }
                }

                if (file != default)
                {
                    await file.FlushAsync(token);
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
            if (result.Count <= 0 && files.Count <= 0)
            {
                ArrayPool<byte>.Shared.Return(outputBuffer);
                return FormDictionary.Empty;
            }
            return new(outputBuffer, result.ToFrozenDictionary(), files.ToFrozenDictionary());
        }
        catch
        {
            ArrayPool<byte>.Shared.Return(outputBuffer);
            throw;
        }
        finally
        {
            file?.Dispose();
        }
    }

    private static byte[] ConvertBoundary(ReadOnlySpan<char> boundary)
    {
        var estimate = (boundary.Length * 4) + 2;
        byte[]? rented = null;
        var bytes = estimate > 256
            ? (rented = ArrayPool<byte>.Shared.Rent(estimate))
            : stackalloc byte[estimate];
        bytes[0] = (byte)'-';
        bytes[1] = (byte)'-';
        Utf8.FromUtf16(boundary, bytes.Slice(2), out _, out var bytesWritten);
        return bytes.Slice(0, bytesWritten + 2).ToArray();
    }

    private static bool Process(
        ref ReadOnlySequence<byte> buffer,
        bool isFinalBlock,
        ReadOnlySpan<byte> boundary,
        Dictionary<ulong, Values<Range>> dictionary,
        Dictionary<ulong, Values<FormFileInternal>> files,
        ref byte[] outputBuffer,
        ref int movingIndex,
        ref FileStream? file,
        ref ulong keyHash,
        ref Range fileName,
        ref Range contentType,
        ref bool headersDone)
    {
        if (buffer.IsSingleSegment)
        {
            var result = ProcessFast(buffer.FirstSpan,
                isFinalBlock,
                boundary,
                out var consumed,
                dictionary,
                files,
                ref outputBuffer,
                ref movingIndex,
                ref file,
                ref keyHash,
                ref fileName,
                ref contentType,
                ref headersDone);

            buffer = buffer.Slice(consumed);
            return result;
        }

        return ProcessSlow(ref buffer,
            isFinalBlock,
            boundary,
            dictionary,
            files,
            ref outputBuffer,
            ref movingIndex,
            ref file,
            ref keyHash,
            ref fileName,
            ref contentType,
            ref headersDone);
    }

    private static bool ProcessSlow(
        ref ReadOnlySequence<byte> buffer,
        bool isFinalBlock,
        ReadOnlySpan<byte> boundary,
        Dictionary<ulong, Values<Range>> dictionary,
        Dictionary<ulong, Values<FormFileInternal>> files,
        ref byte[] outputBuffer,
        ref int movingIndex,
        ref FileStream? file,
        ref ulong keyHash,
        ref Range fileName,
        ref Range contentType,
        ref bool headersDone)
    {
        var sequenceReader = new SequenceReader<byte>(buffer);

        while (true)
        {
            // process headers
            while (!headersDone)
            {
                if (!TryReadPastNewline(ref sequenceReader, out var header))
                {
                    buffer = buffer.Slice(isFinalBlock ? buffer.End : sequenceReader.Position);
                    return false;
                }
                if (keyHash != default && header.IsEmpty)
                {
                    headersDone = true;
                    break;
                }
                ProcessHeader(header, ref fileName, ref contentType, ref keyHash, ref outputBuffer, ref movingIndex);
            }

            // create file
            if (contentType.End.Value != 0 && file == null)
            {
                ref var values = ref CollectionsMarshal.GetValueRefOrAddDefault(files, keyHash, out _);
                var path = Path.GetTempFileName();
                file = File.OpenWrite(path);
                values = values.Add(new FormFileInternal(fileName, contentType, path));
                contentType = default;
                fileName = default;
                keyHash = default;
                continue;
            }

            if (!sequenceReader.TryReadTo(out ReadOnlySequence<byte> value, boundary))
            {
                break;
            }

            headersDone = false;

            if (HandleValue(dictionary, ref outputBuffer, ref movingIndex, file, ref keyHash, value))
            {
                buffer = buffer.Slice(sequenceReader.Consumed);
                return true;
            }
        }

        if (file != default && sequenceReader.UnreadSequence.Length > boundary.Length)
        {
            var length = sequenceReader.UnreadSequence.Length - boundary.Length;
            var fileSequence = sequenceReader.UnreadSequence.Slice(0, length);
            foreach (var mem in fileSequence)
            {
                file.Write(mem.Span);
            }
            buffer = buffer.Slice(fileSequence.End);
            return false;
        }

        buffer = buffer.Slice(isFinalBlock
            ? buffer.End
            : sequenceReader.Position);

        return false;
    }

    private static void ProcessHeader(ReadOnlySequence<byte> header, ref Range fileNameRange, ref Range contentTypeRange, ref ulong keyHash, ref byte[] outputBuffer, ref int movingIndex)
    {
        if (header.IsSingleSegment)
        {
            ProcessHeader(header.FirstSpan, ref fileNameRange, ref contentTypeRange, ref keyHash, ref outputBuffer, ref movingIndex);
            return;
        }

        var headerReader = new SequenceReader<byte>(header);

        while (headerReader.TryReadTo(out ReadOnlySequence<byte> headerPart, ";"u8))
        {
            ProcessHeaderPart(headerPart, ref fileNameRange, ref contentTypeRange, ref keyHash, ref outputBuffer, ref movingIndex);
        }

        ProcessHeaderPart(headerReader.UnreadSequence, ref fileNameRange, ref contentTypeRange, ref keyHash, ref outputBuffer, ref movingIndex);
    }

    private static void ProcessHeaderPart(ReadOnlySequence<byte> headerPart, ref Range fileNameRange, ref Range contentTypeRange, ref ulong keyHash, ref byte[] outputBuffer, ref int movingIndex)
    {
        if (headerPart.IsSingleSegment)
        {
            ProcessHeaderPart(headerPart.FirstSpan, ref fileNameRange, ref contentTypeRange, ref keyHash, ref outputBuffer, ref movingIndex);
            return;
        }

        var headerPartReader = new SequenceReader<byte>(headerPart);

        if (headerPartReader.TryReadTo(out ReadOnlySequence<byte> _, "filename="u8))
        {
            var seq = headerPartReader.UnreadSequence;
            if (seq.IsSingleSegment)
            {
                fileNameRange = Write(seq.FirstSpan.Trim((byte)'"'), ref outputBuffer, ref movingIndex);
                return;
            }
            fileNameRange = Write(Trim(seq, (byte)'"'), ref outputBuffer, ref movingIndex);
            return;
        }

        if (headerPartReader.TryReadTo(out ReadOnlySequence<byte> _, "name="u8))
        {
            var seq = headerPartReader.UnreadSequence;
            if (seq.IsSingleSegment)
            {
                keyHash = GetHash(seq.FirstSpan.Trim((byte)'"'));
                return;
            }
            keyHash = GetHash(Trim(seq, (byte)'"'));
            return;
        }

        if (headerPartReader.TryReadTo(out ReadOnlySequence<byte> _, "Content-Type:"u8))
        {
            var seq = headerPartReader.UnreadSequence;
            if (seq.IsSingleSegment)
            {
                contentTypeRange = Write(seq.FirstSpan.Trim((byte)' '), ref outputBuffer, ref movingIndex);
                return;
            }
            contentTypeRange = Write(Trim(seq, (byte)' '), ref outputBuffer, ref movingIndex);
            return;
        }
    }


    // Fast parsing for single span in ReadOnlySequence
    private static bool ProcessFast(
        ReadOnlySpan<byte> span,
        bool isFinalBlock,
        ReadOnlySpan<byte> boundary,
        out int consumed,
        Dictionary<ulong, Values<Range>> formValues,
        Dictionary<ulong, Values<FormFileInternal>> files,
        ref byte[] outputBuffer,
        ref int movingIndex,
        ref FileStream? formFile,
        ref ulong keyHash,
        ref Range fileName,
        ref Range contentType,
        ref bool headersDone)
    {
        ReadOnlySpan<byte> header = default;
        ReadOnlySpan<byte> value = default;
        consumed = 0;

        while (true)
        {
            while (!headersDone)
            {
                if (!TryReadPastNewline(ref span, ref consumed, ref header))
                {
                    if (isFinalBlock)
                    {
                        consumed += span.Length;
                    }
                    return false;
                }
                if (keyHash != default && header.IsEmpty)
                {
                    headersDone = true;
                    break;
                }
                ProcessHeader(header, ref fileName, ref contentType, ref keyHash, ref outputBuffer, ref movingIndex);
            }

            if (contentType.End.Value != 0 && formFile == null)
            {
                ref var values = ref CollectionsMarshal.GetValueRefOrAddDefault(files, keyHash, out _);
                var path = Path.GetTempFileName();
                formFile = File.OpenWrite(path);
                values = values.Add(new FormFileInternal(fileName, contentType, path));
                contentType = default;
                fileName = default;
                keyHash = default;
            }

            if (!TryReadTo(boundary, ref span, ref consumed, ref value))
            {
                break;
            }

            headersDone = false;
            if (HandleValue(formValues, ref outputBuffer, ref movingIndex, formFile, ref keyHash, value)) return true;
        }

        if (formFile != default && span.Length > boundary.Length)
        {
            var filePart = span[..^boundary.Length];
            formFile.Write(filePart);
            consumed += filePart.Length;
            span = span[filePart.Length..];
        }

        if (isFinalBlock)
        {
            consumed += span.Length;
        }

        return false;
    }

    private static bool HandleValue(Dictionary<ulong, Values<Range>> formValues, ref byte[] outputBuffer, ref int movingIndex, FileStream? formFile, ref ulong keyHash, ReadOnlySequence<byte> value)
    {
        if (value.IsSingleSegment)
        {
            return HandleValue(formValues, ref outputBuffer, ref movingIndex, formFile, ref keyHash, value.FirstSpan);
        }
        value = TrimNewLineEnd(value);
        if (formFile != default)
        {
            foreach (var item in value)
            {
                formFile.Write(item.Span);
            }
            return true;
        }
        ref var simpleValues = ref CollectionsMarshal.GetValueRefOrAddDefault(formValues, keyHash, out _);
        var range = Write(value, ref outputBuffer, ref movingIndex);
        simpleValues = simpleValues.Add(range);
        keyHash = default;
        return false;
    }

    private static ReadOnlySequence<byte> TrimNewLineEnd(ReadOnlySequence<byte> value)
    {
        if (!value.IsEmpty && value.Slice(value.Length - 1).FirstSpan[0] == (byte)'\n')
        {
            value = value.Slice(0, value.Length - 1);
        }
        if (!value.IsEmpty && value.Slice(value.Length - 1).FirstSpan[0] == (byte)'\r')
        {
            value = value.Slice(0, value.Length - 1);
        }

        return value;
    }

    private static bool HandleValue(Dictionary<ulong, Values<Range>> formValues, ref byte[] outputBuffer, ref int movingIndex, FileStream? formFile, ref ulong keyHash, ReadOnlySpan<byte> value)
    {
        value = TrimNewLineEnd(value);
        if (formFile != default)
        {
            formFile.Write(value);
            return true;
        }
        ref var simpleValues = ref CollectionsMarshal.GetValueRefOrAddDefault(formValues, keyHash, out _);
        var range = Write(value, ref outputBuffer, ref movingIndex);
        simpleValues = simpleValues.Add(range);
        keyHash = default;
        return false;
    }

    private static ReadOnlySpan<byte> TrimNewLineEnd(ReadOnlySpan<byte> value)
    {
        if (!value.IsEmpty && value[^1] == (byte)'\n')
        {
            value = value[..^1];
        }
        if (!value.IsEmpty && value[^1] == (byte)'\r')
        {
            value = value[..^1];
        }
        return value;
    }

    private static ReadOnlySequence<byte> Trim(ReadOnlySequence<byte> bytes, byte singleByte)
    {
        if (bytes.Slice(bytes.Length - 1).FirstSpan[0] == singleByte)
        {
            bytes = bytes.Slice(0, bytes.Length - 1);
        }
        if (bytes.Slice(0, 1).FirstSpan[0] == singleByte)
        {
            bytes = bytes.Slice(1);
        }
        return bytes;
    }

    private static ulong GetHash(ReadOnlySequence<byte> key)
    {
        if (key.IsSingleSegment) return GetHash(key.FirstSpan);

        if (key.Length <= StackAllocThreshold)
        {
            Span<byte> buffer = stackalloc byte[(int)key.Length];
            key.CopyTo(buffer);

            return XxHash3.HashToUInt64(buffer);
        }

        var rented = ArrayPool<byte>.Shared.Rent((int)key.Length);
        try
        {
            return XxHash3.HashToUInt64(rented);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private static ulong GetHash(ReadOnlySpan<byte> key) => XxHash3.HashToUInt64(key);

    private static void ProcessHeader(
        ReadOnlySpan<byte> header,
        ref Range fileNameRange,
        ref Range contentTypeRange,
        ref ulong keyHash,
        ref byte[] outputBuffer,
        ref int movingIndex)
    {
        if (header.IsEmpty) return;

        foreach (var range in header.Split(";"u8))
        {
            ProcessHeaderPart(header[range], ref fileNameRange, ref contentTypeRange, ref keyHash, ref outputBuffer, ref movingIndex);
        }
    }

    private static void ProcessHeaderPart(
        ReadOnlySpan<byte> headerPart,
        ref Range fileNameRange,
        ref Range contentTypeRange,
        ref ulong keyHash,
        ref byte[] outputBuffer,
        ref int movingIndex)
    {
        const byte Space = (byte)' ';
        const byte Quote = (byte)'"';
        var namePrefix = "name="u8;
        var fileNameBytes = "filename="u8;
        var contentTypeBytes = "Content-Type:"u8;

        headerPart = headerPart.Trim(Space);
        if (headerPart.IsEmpty) return;

        if (headerPart.StartsWith(contentTypeBytes))
        {
            var contentType = headerPart.Slice(contentTypeBytes.Length).Trim(Space);
            contentTypeRange = Write(contentType, ref outputBuffer, ref movingIndex);
        }
        else if (headerPart.StartsWith(fileNameBytes))
        {
            var fileName = headerPart.Slice(fileNameBytes.Length).Trim(Quote);
            fileNameRange = Write(fileName, ref outputBuffer, ref movingIndex);
        }
        else if (headerPart.StartsWith(namePrefix))
        {
            var key = headerPart.Slice(namePrefix.Length).Trim(Quote);
            keyHash = XxHash3.HashToUInt64(key);
        }
    }

    static bool TryReadTo(ReadOnlySpan<byte> search, ref ReadOnlySpan<byte> span, ref int consumed, ref ReadOnlySpan<byte> before, bool readPast = true)
    {
        var index = span.IndexOf(search);

        if (index < 0)
        {
            return false;
        }

        before = span.Slice(0, index);
        var next = readPast ? index + search.Length : index;
        span = span.Slice(next);
        consumed += next;
        return true;
    }

    static bool TryReadPastNewline(ref ReadOnlySpan<byte> span, ref int consumed, ref ReadOnlySpan<byte> before) =>
        TryReadTo("\r\n"u8, ref span, ref consumed, ref before)
        || TryReadTo("\n"u8, ref span, ref consumed, ref before);

    static bool TryReadPastNewline(ref SequenceReader<byte> reader, out ReadOnlySequence<byte> before)
    {
        if (reader.TryReadTo(out before, "\r\n"u8)) return true;
        if (reader.TryReadTo(out before, (byte)'\n')) return true;
        before = default;
        return false;
    }

    private static Range Write(ReadOnlySpan<byte> bytes, ref byte[] outputBuffer, ref int movingIndex)
    {
        Grow(bytes.Length, ref outputBuffer, movingIndex);
        bytes.CopyTo(outputBuffer.AsSpan(movingIndex));
        return new(movingIndex, movingIndex += bytes.Length);
    }

    private static Range Write(ReadOnlySequence<byte> bytes, ref byte[] outputBuffer, ref int movingIndex)
    {
        if (bytes.IsSingleSegment) return Write(bytes.FirstSpan, ref outputBuffer, ref movingIndex);
        Grow((int)bytes.Length, ref outputBuffer, movingIndex);
        bytes.CopyTo(outputBuffer.AsSpan(movingIndex));
        return new(movingIndex, movingIndex += (int)bytes.Length);
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
}
