using System.Collections.Immutable;

namespace MinimalHtml
{
    public delegate ValueTask<Asset> GetAsset(string id);
    public delegate ValueTask<ImmutableDictionary<string, Asset>> GetAssetDictionary();

    public record Asset(string Src, string? Integrity, ImmutableArray<Asset> Imports);

    public static class Assets
    {
        public static readonly GetAsset Noop = (id) => new ValueTask<Asset>(new Asset(id, null, []));

        public static async ValueTask<ImmutableDictionary<string, Asset>> Combine(this GetAssetDictionary left, GetAssetDictionary right)
        {
            var leftTask = left();
            var rightDict = await right();
            var leftDict = await leftTask;

            var result = new Dictionary<string, Asset>();

            Asset Combine(Asset value)
            {
                var src = TrimUrl(value.Src);
                var integrity = value.Integrity;
                var imports = value.Imports;
                if (rightDict.TryGetValue(src, out var rightVal))
                {
                    src = rightVal.Src;
                    integrity = rightVal.Integrity;
                }
                if (value.Imports.Length > 0)
                {
                    var importList = new List<Asset>();
                    foreach (var imported in value.Imports)
                    {
                        importList.Add(Combine(imported));
                    }
                    imports = [.. importList];
                }
                return new(src, integrity, imports);
            }

            foreach (var (key, value) in leftDict)
            {
                result[TrimUrl(key)] = Combine(value);
            }

            return result.ToImmutableDictionary();
        }

        public static string TrimUrl(string s)
        {
            if(s == null) return "";
            if(s.StartsWith('/')) return s;
            var trimmed = s.AsSpan().TrimStart('~').TrimStart('/');
            return $"/{trimmed}";
        }
    }
}
