using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ZodNet.ModelGenerator
{
    internal class ModelProvider
    {
        internal static IncrementalValuesProvider<ConfigDeclaration> Register(IncrementalGeneratorInitializationContext context) => context.SyntaxProvider.ForAttributeWithMetadataName(
            "ZodNet.ZodAttribute",
            static (node, _) => true,
            static (ctx, _) => GetConfigDeclaration(ctx));

        private static ConfigDeclaration GetConfigDeclaration(GeneratorAttributeSyntaxContext ctx)
        {
            if (ctx.TargetNode is not VariableDeclaratorSyntax { Initializer.Value: ParenthesizedLambdaExpressionSyntax { ExpressionBody: InvocationExpressionSyntax ma } }) return default;
            var containingClass = ctx.TargetSymbol.ContainingType;
            var accessiblity = GetAccessibility(containingClass.DeclaredAccessibility);
            var containingNamespace = containingClass.ContainingNamespace?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
            if (containingNamespace == null) return default;
            var values = ExpressionGetter(ma).Reverse();
            var pairs = GetPairs(values).ToArray();

            return new ConfigDeclaration(containingNamespace, containingClass.Name, accessiblity, new(pairs));
        }

        private static string GetAccessibility(Accessibility @enum) => @enum switch
        {
            Accessibility.Private => "private",
            Accessibility.Internal => "internal",
            _ => "public"
        };

        private static IEnumerable<CSharpSyntaxNode> ExpressionGetter(ExpressionSyntax expression)
        {
            if (expression is InvocationExpressionSyntax i)
            {
                yield return i.ArgumentList;
                foreach (var item in ExpressionGetter(i.Expression))
                {
                    yield return item;
                }
            }
            else if (expression is MemberAccessExpressionSyntax m)
            {
                yield return m.Name;
                foreach (var item in ExpressionGetter(m.Expression))
                {
                    yield return item;
                }
            }
        }

        private static IEnumerable<Pair> GetPairs(IEnumerable<CSharpSyntaxNode> values)
        {
            SimpleNameSyntax? left = default;
            foreach (var item in values)
            {
                if (item is SimpleNameSyntax s)
                {
                    left = s;
                }
                else if (item is ArgumentListSyntax args && left is not null)
                {
                    var arr = args.Arguments.Select(x => x.ToString()).ToArray();
                    yield return new(left.ToString(), new(arr));
                }
            }
        }
    }
}
