using System.Collections.Immutable;
using System.Text.Json;

namespace MinimalHtml.Vite
{
    public class ViteManifestResolver(string manifestPath)
    {
        public const string DefaultRelativeManifestPath = ".vite/manifest.json";

        public async ValueTask<ImmutableDictionary<string, Asset>> GetAssets()
        {
            var result = new Dictionary<string, Asset>();
            var importDict = new Dictionary<string, Asset>();
            await using var file = File.OpenRead(manifestPath);
            using var doc = await JsonDocument.ParseAsync(file);

            var props = doc.RootElement.EnumerateObject().ToDictionary(x => x.Name, x => x.Value);

            while (props.Count > 0)
            {
                var first = props.FirstOrDefault();
                props.Remove(first.Key);
                var isEntry = first.Value.TryGetProperty("isEntry"u8, out var e) && e.GetBoolean();
                var src = first.Value.GetProperty("file"u8).GetString() ?? first.Key;
                var asset = new Asset(src, null, HandleImports(first.Value));
                importDict[first.Key] = asset;
                if (isEntry)
                {
                    result[first.Key] = asset;
                }
            }

            return result.ToImmutableDictionary();

            ImmutableArray<Asset> HandleImports(JsonElement element)
            {
                if (element.TryGetProperty("imports"u8, out var imports))
                {
                    var list = new List<Asset>();
                    foreach (var importKey in imports.EnumerateArray().Select(i => i.GetString()))
                    {
                        if (!importDict.TryGetValue(importKey, out var importedAsset))
                        {
                            var import = props[importKey];
                            props.Remove(importKey);
                            importedAsset = new Asset(importKey, null, HandleImports(import));
                            importDict[importKey] = importedAsset;
                            var isEntry = import.TryGetProperty("isEntry"u8, out var e) && e.GetBoolean();
                            if (isEntry)
                            {
                                result[importKey] = importedAsset;
                            }
                        }
                        list.Add(importedAsset);
                    }
                    return [.. list];
                }
                return [];
            }
        }
    }
}
