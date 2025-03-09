using System.Collections.Immutable;
using System.Text.Json;

namespace MinimalHtml.AspNetCore
{
    public class AspNetManifestResolver(string manifestPath)
    {
        public async ValueTask<ImmutableDictionary<string, Asset>> Parse()
        {
            ArgumentNullException.ThrowIfNull(manifestPath);
            var result = new Dictionary<string, Asset>();
            await using var stream = File.OpenRead(manifestPath);
            using var doc = await JsonDocument.ParseAsync(stream);

            if (doc.RootElement.TryGetProperty("Endpoints"u8, out var endpoints))
            {
                foreach (var descriptor in endpoints.EnumerateArray())
                {
                    if (descriptor.TryGetProperty("Selectors"u8, out var selectors))
                    {
                        var enumerator = selectors.EnumerateArray();
                        try
                        {
                            if (enumerator.MoveNext()) continue;
                        }
                        finally
                        {
                            enumerator.Dispose();
                        }
                    }

                    var isEncoded = false;

                    if (descriptor.TryGetProperty("ResponseHeaders"u8, out var headers))
                    {
                        foreach (var item in headers.EnumerateArray())
                        {
                            if (item.TryGetProperty("Name"u8, out var name) && name.ValueEquals("Content-Encoding"u8))
                            {
                                isEncoded = true;
                                break;
                            }
                        }
                    }

                    if (isEncoded) continue;

                    if (descriptor.TryGetProperty("EndpointProperties"u8, out var properties) && descriptor.TryGetProperty("Route"u8, out var route))
                    {
                        string? label = null;
                        string? integrity = null;
                        var foundProperties = 0;

                        foreach (var property in properties.EnumerateArray())
                        {
                            if (property.TryGetProperty("Name"u8, out var name))
                            {
                                if (name.ValueEquals("label"u8))
                                {
                                    label = property.GetProperty("Value").GetString();
                                    foundProperties++;
                                }
                                else if (name.ValueEquals("integrity"u8))
                                {
                                    integrity = property.GetProperty("Value").GetString();
                                    foundProperties++;
                                }
                                if (foundProperties == 2)
                                {
                                    break;
                                }
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(label) && !label.EndsWith(".map"))
                        {
                            var asset = new Asset(TrimUrl(route.GetString()!), integrity, []);
                            result[TrimUrl(label)] = asset;
                        }
                    }
                }
            }

            return result.ToImmutableDictionary();
        }

        internal static string TrimUrl(string s)
        {
            if (s == null) return "";
            if (s.StartsWith('/')) return s;
            var trimmed = s.AsSpan().TrimStart('~').TrimStart('/');
            return $"/{trimmed}";
        }
    }
}
