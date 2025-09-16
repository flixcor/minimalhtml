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
                Asset asset;
                if(src.EndsWith(".module.js") && first.Value.GetProperty("src"u8).GetString().EndsWith(".module.css"))
                {
                    asset = HandleImports(first.Value)[0];
                }
                else
                {
                    asset = new Asset(src, null, HandleImports(first.Value));
                }
                importDict[first.Key] = asset;
                if (isEntry || (!src.EndsWith(".js") && !src.EndsWith(".css")))
                {
                    result[first.Key] = asset;
                }
            }

            return result.ToImmutableDictionary();

            ImmutableArray<Asset> HandleImports(JsonElement element)
            {
                var list = new List<Asset>();
                if (element.TryGetProperty("imports"u8, out var imports))
                {
                    foreach (var import in imports.EnumerateArray())
                    {
                        list.Add(GetImport(import.GetString()));
                    }
                }
                if (element.TryGetProperty("assets"u8, out imports))
                {
                    foreach (var import in imports.EnumerateArray())
                    {
                        list.Add(GetImport(import.GetString()));
                    }
                }
                if (element.TryGetProperty("css"u8, out imports))
                {
                    foreach (var import in imports.EnumerateArray())
                    {
                        list.Add(GetImport(import.GetString()));
                    }
                }
                return [.. list];
            }

            Asset GetImport(string importKey)
            {
                if (importDict.TryGetValue(importKey, out var importedAsset))
                {
                    return importedAsset;
                }

                if (props.TryGetValue(importKey, out var import))
                {
                    importedAsset = new Asset(importKey, null, HandleImports(import));
                    props.Remove(importKey);
                    var isEntry = import.TryGetProperty("isEntry"u8, out var e) && e.GetBoolean();
                    if (isEntry)
                    {
                        result[importKey] = importedAsset;
                    }
                }
                else
                {
                    importedAsset = new Asset(importKey, null, []);
                }

                importDict[importKey] = importedAsset;
                return importedAsset;
            }
        }
    }
}
