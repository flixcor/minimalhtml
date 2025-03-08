using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MinimalHtml.CssModules
{
    [Generator]
    public class CssGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var additionalTexts = context.AdditionalTextsProvider
                .Where(static f => f.Path.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
                .Select(static (f, t) => (f.Path, Text: f.GetText(t)))
                .Collect();

            var rootDirectory = context.AnalyzerConfigOptionsProvider
                .Select((x, _) => x.GlobalOptions.TryGetValue("build_property.projectdir", out var dir) ? dir : "");

            var all = additionalTexts.Combine(rootDirectory);

            context.RegisterSourceOutput(all, (sourceContext, data) =>
            {
                var (cssFiles, rootDir) = data;
                var dirSpan = rootDir.AsSpan();
                foreach (var item in cssFiles)
                {
                    var pathSpan = item.Path.AsSpan();
                    var relativePath = Helpers.ToRelative(pathSpan, dirSpan);
                    var qualifiedClassName = Helpers.RelativePathToQualifiedClassName(relativePath);
                    var isModule = item.Path.EndsWith(".module.css");
                    if (isModule)
                    {
                        qualifiedClassName = qualifiedClassName.Replace(".module", "");
                    }
                    var text = AddScriptRegisterFunction(item.Path, qualifiedClassName, relativePath, item.Text, isModule);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        sourceContext.AddSource(qualifiedClassName + ".css.g.cs", SourceText.From(text, Encoding.UTF8));
                    }
                }
            });
        }

        private static string AddScriptRegisterFunction(string fullPath, string className, ReadOnlySpan<char> fileName, SourceText? sourceText, bool isModule)
        {
            string? o;
            byte[] brotli = [];
            IReadOnlyDictionary<string, string> classes = ImmutableDictionary<string, string>.Empty;
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var command = isWindows
                ? "lightningcss.cmd"
                : "lightningcss";

            var info = new ProcessStartInfo
            {
                FileName = command,
                Arguments = $" --error-recovery --custom-media --targets \">= 0.25%\" --minify{(isModule ? " --css-modules true" : "")} {fullPath}",
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                UseShellExecute = false,
            };
            using (var process = Process.Start(info))
            {
                process.WaitForExit();
                o = process.StandardOutput.ReadToEnd().Trim();
                if (isModule)
                {
                    using var json = JsonDocument.Parse(o);
                    o = json.RootElement.GetProperty("code").GetString()?.Trim();
                    static string ToCamelCase(string kebabCase) => kebabCase.Split(new[] { "-" }, StringSplitOptions.RemoveEmptyEntries).Select(s => char.ToUpperInvariant(s[0]) + s.Substring(1, s.Length - 1)).Aggregate(string.Empty, (s1, s2) => s1 + s2);
                    classes = json.RootElement.GetProperty("exports").EnumerateObject().ToDictionary(x => ToCamelCase(x.Name), x => x.Value.GetProperty("name").GetString()!);
                }
            }
            var brInfo = new ProcessStartInfo
            {
                FileName = isWindows ? "powershell" : "sh",
                Arguments = isWindows ? $"'{o}' | brotli --stdout -f" : null,
                RedirectStandardInput = !isWindows,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                UseShellExecute = false,
            };
            using (var process = Process.Start(brInfo))
            {
                if (!isWindows)
                {
                    process.StandardInput.WriteLine($"echo \"{o}\" | brotli -f --stdout");
                    process.StandardInput.Close();
                }
                process.WaitForExit();
                using var mem = new MemoryStream();
                process.StandardOutput.BaseStream.CopyTo(mem);
                mem.Seek(0, SeekOrigin.Begin);
                brotli = mem.ToArray();
            }
            int i = className.LastIndexOf('.');
            var ns = className.Substring(0, i);
            var classOnly = className.Substring(i + 1);
            var skipFirstFolder = fileName.Slice(fileName.IndexOfAny('\\', '/'));
            var path = new char[skipFirstFolder.Length];
            for (int j = 0; j < skipFirstFolder.Length; j++)
            {
                var c = skipFirstFolder[j];
                path[j] = c == '\\' ? '/' : c;
            }
            var pathStr = new string(path);
            var hash = System.IO.Hashing.XxHash3.HashToUInt64(brotli).ToString("X");
            var text = $$"""""""
            using MinimalHtml;
            using static MinimalHtml.HtmlTemplate;
            using System.Buffers;
            namespace {{ns}};
            public partial class {{classOnly}}
            {
                public partial class Css: ICss
                {
                    public static string Path { get; } = "{{pathStr}}";
            
                    public static string Hash() => "{{hash}}";

                    public static ValueTask<bool> Link(Page page, CancellationToken token) 
                    {
                        var span = """<link rel="stylesheet" href="{{pathStr}}?v={{hash}}"/>"""u8;
                        span.CopyTo(page.GetSpan(span.Length));
                        page.Advance(span.Length);
                        return new(false);
                    }
            
                    public static void RenderBrotli(IBufferWriter<byte> writer)
                    {
                        ReadOnlySpan<byte> span = [{{string.Join(",", brotli)}}];
                        span.CopyTo(writer.GetSpan(span.Length));
                        writer.Advance(span.Length);
                    }

                    public static void RenderUncompressed(IBufferWriter<byte> writer)
                    {
                        ReadOnlySpan<byte> span = "{{o}}"u8;
                        span.CopyTo(writer.GetSpan(span.Length));
                        writer.Advance(span.Length);
                    }
                }{{(classes.Count == 0 ? "" : $$"""
            

                public static class Classes 
                {
                    {{string.Join("\n        ", classes.Select(kv => $"""public const string {kv.Key} = "{kv.Value}";"""))}}
                }
            """)}}
            }
            """"""";
            return text;
        }
    }
}
