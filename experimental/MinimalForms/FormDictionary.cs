
using System.IO.Hashing;
using System.Buffers;
using System.Collections.Frozen;

namespace MinimalForms;

internal readonly record struct FormFileInternal(Range FileName, Range ContentType, string Path);

public readonly record struct FormFile
{
    private readonly string _path;

    public FormFile(ReadOnlyMemory<byte> fileName, ReadOnlyMemory<byte> contentType, string path)
    {
        FileName = fileName;
        ContentType = contentType;
        _path = path;
    }

    public ReadOnlyMemory<byte> FileName { get; }
    public ReadOnlyMemory<byte> ContentType { get; }
    
    public Stream OpenReadStream() => File.Exists(_path) ? File.OpenRead(_path) : Stream.Null;
    public long Length => File.Exists(_path) ? new FileInfo(_path).Length : 0;
}

public sealed class FormDictionary : IDisposable
{
    internal FormDictionary(byte[] buffer, FrozenDictionary<ulong, Values<Range>> dict, FrozenDictionary<ulong, Values<FormFileInternal>> files)
    {
        _buffer = buffer;
        _dict = dict;
        _files = files;
        _memory = buffer;
    }

    public static readonly FormDictionary Empty = new([], FrozenDictionary<ulong, Values<Range>>.Empty, FrozenDictionary<ulong, Values<FormFileInternal>>.Empty);
    private byte[] _buffer;
    private readonly FrozenDictionary<ulong, Values<Range>> _dict;
    private readonly FrozenDictionary<ulong, Values<FormFileInternal>> _files;
    private ReadOnlyMemory<byte> _memory;

    public Values<ReadOnlyMemory<byte>> this[ReadOnlySpan<byte> key] => TryGetValue(key, out var values) ? values : default;

    public int Count => _buffer.Length == 0 ? 0 : _dict.Count + _files.Count;

    public void Dispose()
    {
        if (_buffer.Length > 0)
        {
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = [];
            _memory = default;
        }
        foreach (var item in _files.Values)
        {
            foreach (var file in item)
            {
                File.Delete(file.Path);
            }
        }
    }

    public bool TryGetValue(ReadOnlySpan<byte> key, out Values<ReadOnlyMemory<byte>> value)
    {
        value = default;
        if(_buffer.Length == 0) return false;
        var hash = XxHash3.HashToUInt64(key);
        if (!_dict.TryGetValue(hash, out var values)) return false;
        foreach (var range in values)
        {
            var memory = _memory[range];
            value = value.Add(memory);
        }
        return true;
    }

    public bool TryGetFile(ReadOnlySpan<byte> key, out Values<FormFile> value)
    {
        value = default;
        if(_buffer.Length == 0) return false;
        var hash = XxHash3.HashToUInt64(key);
        if (!_files.TryGetValue(hash, out var values)) return false;
        foreach (var f in values)
        {
            if(!File.Exists(f.Path) || new FileInfo(f.Path).Length == 0) continue;
            var file = new FormFile(_memory[f.FileName], _memory[f.ContentType], f.Path);
            value = value.Add(file);
        }
        return value.Count > 0;
    }
}
