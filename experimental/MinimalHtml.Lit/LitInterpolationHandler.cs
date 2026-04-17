using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using Jint.Native;

namespace MinimalHtml.Lit;

internal static class RendererPool
{
    internal static readonly ConcurrentBag<LitRenderer> Pool = new();
}

[InterpolatedStringHandler]
public readonly ref struct LitInterpolationHandler
{
    internal readonly List<JsValue> _literals;
    internal readonly List<JsValue> _values;
    
    public LitInterpolationHandler(int literalCount, int valueCount)
    {
        _literals = new (literalCount);
        _values =  new (valueCount);
    }

    public void AppendLiteral(string literal) => _literals.Add(literal);

    public void AppendFormatted(JsValue value) => _values.Add(value);
}

public static class LitExtensions
{
    public static ValueTask<FlushResult> Lit(this PipeWriter writer,
        [StringSyntax("Html")] LitInterpolationHandler handler) =>
            Render(writer, handler._literals, handler._values);

    private static async ValueTask<FlushResult> Render(PipeWriter writer, List<JsValue> literals, List<JsValue> values)
    {
        var renderer = RendererPool.Pool.TryTake(out var r) 
            ? r 
            : new LitRenderer(LitRenderer.Default ?? throw new InvalidOperationException("LitRenderer.Default must be set before using LitInterpolationHandler. Call UseLit() in your Startup/Program class."));
        try
        {
            return await renderer.Render(writer, literals, values);
        }
        finally
        {
            RendererPool.Pool.Add(renderer);
        }
    }
}


