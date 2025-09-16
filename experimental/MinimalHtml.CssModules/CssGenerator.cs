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
                .Where(static f => f.Path.EndsWith(".module.css.json", StringComparison.OrdinalIgnoreCase))
                .Select(static (f, t) => (f.Path, Text: f.GetText(t).ToString()))
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
                    qualifiedClassName = qualifiedClassName.Replace(".module.css", "");
                    var text = AddScriptRegisterFunction(qualifiedClassName, item.Text);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        sourceContext.AddSource(qualifiedClassName + ".g.cs", SourceText.From(text, Encoding.UTF8));
                    }
                }
            });
        }

        private static string AddScriptRegisterFunction(string className, string sourceText)
        {
            using var json = JsonDocument.Parse(sourceText);
            var classes = json.RootElement.EnumerateObject().ToDictionary(x => x.Name, x => x.Value.GetString()!);
            int i = className.LastIndexOf('.');
            var ns = className.Substring(0, i);
            var classOnly = className.Substring(i + 1);
            var text = $$"""""""
            namespace {{ns}};
            public partial class {{classOnly}}
            {
                {{(classes.Count == 0 ? "" : $$"""
            

                public static class Classes 
                {
                    {{string.Join("\n        ", classes.Select(kv => $"""public static ReadOnlySpan<byte> {kv.Key}() => "{kv.Value}"u8;"""))}}
                }
            """)}}
            }
            """"""";
            return text;
        }
    }
}
