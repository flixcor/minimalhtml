using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using Jint.Native;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalHtml.Lit;

[InterpolatedStringHandler]
public readonly ref struct LitInterpolationHandler
{
    private readonly LitRenderer _renderer;
    private readonly JsArray _literals;
    private readonly JsArray _values;
    public LitInterpolationHandler(int literalCount, int valueCount, LitRenderer renderer)
    {
        _renderer = renderer;
        _literals = _renderer.GetJsArray(literalCount);
        _values = _renderer.GetJsArray(valueCount);
    }

    public LitInterpolationHandler(int literalCount, int valueCount): this(literalCount, valueCount, LitRenderer.Default ?? throw new InvalidOperationException("LitRenderer.Default must be set before using LitInterpolationHandler. Call UseLit() in your Startup/Program class."))
    {
    }

    public void AppendLiteral(string literal) => _literals.Push(literal);

    public void AppendFormatted(JsValue value) => _values.Push(value);

    internal ValueTask<FlushResult> Render(PipeWriter writer) => _renderer.Render(writer, _literals, _values);
}

public static class LitExtensions
{
    public static ValueTask<FlushResult> Lit([StringSyntax("Html")] this PipeWriter writer, LitRenderer renderer, [InterpolatedStringHandlerArgument(nameof(renderer))] LitInterpolationHandler handler) => handler.Render(writer);

    public static ValueTask<FlushResult> Lit([StringSyntax("Html")] this PipeWriter writer, [InterpolatedStringHandlerArgument()] LitInterpolationHandler handler) => handler.Render(writer);

    public static void UseLit(this IServiceCollection services, LitOptions options)
    {
        LitRenderer.Default = new LitRenderer(options);
    }
}


