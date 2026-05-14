using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MinimalHtml.SourceGenerator
{
    [Generator]
    public class TemplateCacheGenerator : IIncrementalGenerator
    {
        private static readonly Regex s_escapeRegex = new(@"\r|\n|""");

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
                "FillTemplateCacheAttribute.g.cs", @"
                namespace MinimalHtml
                {
                    internal class FillTemplateCacheAttribute: System.Attribute {} 
                }"
                ));

            var interpolationProvider = context.SyntaxProvider.CreateSyntaxProvider(IsSyntaxTargetForGeneration, GetSemanticTargetForGeneration).Collect();

            var methodDeclarationProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
                "MinimalHtml.FillTemplateCacheAttribute",
                static (node, _) => true,
                static (ctx, _) => GetMethodDeclaration(ctx)
                );

            // Emit the cache body only in Release. In Debug the body churns on every template-literal edit,
            // which the EnC runtime rejects ("The assembly update failed") and kills the Hot Reload session.
            // Consumers gate the runtime call on IsDevelopment, so an empty Debug body is correct anyway.
            var isReleaseProvider = context.CompilationProvider
                .Select(static (c, _) => c.Options.OptimizationLevel == OptimizationLevel.Release);

            var combined = methodDeclarationProvider.Combine(interpolationProvider).Combine(isReleaseProvider);

            context.RegisterSourceOutput(combined, RegisterThing);
            

            
        }

        private static CacheMethod GetMethodDeclaration(GeneratorAttributeSyntaxContext ctx)
        {
            var containingClass = ctx.TargetSymbol.ContainingType;
            var containingNamespace = containingClass.ContainingNamespace?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
            return new CacheMethod(ctx.TargetSymbol.Name, containingClass.Name, containingNamespace!);
        }

        private IEnumerable<string> GetParts(Interpolation interpolation)
        {
            var indentation = interpolation.StringEndToken.Count(x => x == ' ');
            var regex = new Regex($@"^ {{{indentation}}}", RegexOptions.Multiline);
            foreach (var item in interpolation.Contents)
            {
                var str = s_escapeRegex.Replace(regex.Replace(item, ""), x => x.Value switch
                {
                    "\n" => "\\n",
                    "\r" => "\\r",
                    _ => "\\\""
                });
                if (string.IsNullOrEmpty(str)) continue;
                yield return str;
            }
        }

        

        private void RegisterThing(SourceProductionContext context, ((CacheMethod Method, ImmutableArray<Interpolation> Interpolations) Inner, bool IsRelease) source)
        {
            var body = source.IsRelease
                ? string.Join("\n", source.Inner.Interpolations.SelectMany(GetParts)
                    .Select(str => $$"""
                            MinimalHtml.TemplateHandler.Precompile("{{str}}", "{{str}}"u8.ToArray());
                    """))
                : "";

            var text = $$"""
            namespace {{source.Inner.Method.Namespace}};
            public partial class {{source.Inner.Method.ClassName}}
            {
                public static partial void {{source.Inner.Method.MethodName}}()
                {
            {{body}}
                }
            }
            """;

            context.AddSource("Precompile.g.cs", SourceText.From(text, Encoding.UTF8));
        }

        private static Interpolation GetSemanticTargetForGeneration(GeneratorSyntaxContext context, CancellationToken token)
        {
            var syntax = (context.Node as InterpolatedStringExpressionSyntax)!;
            var contents = syntax.Contents.OfType<InterpolatedStringTextSyntax>().Select(x => x.TextToken.Text).ToArray();
            return new Interpolation(syntax.StringEndToken.Text, new(contents));
        }


        private static bool IsSyntaxTargetForGeneration(SyntaxNode node, CancellationToken token) => node is InterpolatedStringExpressionSyntax interpolated &&
                node.Ancestors()
                .OfType<InvocationExpressionSyntax>()
                .Select(x => x.Expression)
                .OfType<MemberAccessExpressionSyntax>()
                .Any(x=> x.Name.Identifier.Text == "Html");
    }
}
