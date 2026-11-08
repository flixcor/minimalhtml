using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using Jint.Native;

namespace MinimalHtml.Vite.Lit;

[InterpolatedStringHandler]
public readonly ref struct LitInterpolationHandler
{
    internal readonly List<JsValue> _literals;
    internal readonly List<JsValue> _values;

    public LitInterpolationHandler(int literalCount, int valueCount)
    {
        _literals = new(literalCount);
        _values = new(valueCount);
    }

    public void AppendLiteral(string literal) => _literals.Add(literal);

    public void AppendFormatted(JsValue value) => _values.Add(value);
}

public static class LitExtensions
{
    public static ValueTask<FlushResult> Lit(this PipeWriter writer,
        [StringSyntax("Html")] LitInterpolationHandler handler) =>
            (LitRuntime.Current
                ?? throw new InvalidOperationException(
                    "Lit runtime not initialized. Call services.AddLitRenderer() and app.UseMinimalHtmlVite() during startup."))
            .Render(writer, handler._literals, handler._values);
}
