using System.Collections.Immutable;

namespace MinimalHtml.Vite;

public delegate ValueTask<Asset> GetAsset(string id);

public record Asset(string Src, string? Integrity, ImmutableArray<Asset> Imports);

public static class Assets
{
    public static readonly GetAsset Noop = (id) => new ValueTask<Asset>(new Asset(id, null, []));

    internal static string TrimUrl(string s)
    {
        if (s == null) return "";
        if (s.StartsWith('/')) return s;
        var trimmed = s.AsSpan().TrimStart('~').TrimStart('/');
        return $"/{trimmed}";
    }
}
