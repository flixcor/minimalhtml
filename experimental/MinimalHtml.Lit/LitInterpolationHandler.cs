using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using Jint.Native;

namespace MinimalHtml.Lit;

[InterpolatedStringHandler]
public readonly ref struct LitInterpolationHandler
{
    private readonly LitRenderer _renderer;
    private readonly JsArray _literals;
    private readonly JsArray _values;
    public LitInterpolationHandler(int literalCount, int valueCount)
    {
        _renderer = new LitRenderer();
        _literals = _renderer.GetJsArray(literalCount);
        _values = _renderer.GetJsArray(valueCount);
    }

    public void AppendLiteral(string literal) => _literals.Push(literal);

    public void AppendFormatted(JsValue value) => _values.Push(value);

    internal ValueTask<FlushResult> Render(PipeWriter writer) => _renderer.Render(writer, _literals, _values);
}

public static class LitExtensions
{
    public static ValueTask<FlushResult> Lit([StringSyntax("Html")] this PipeWriter writer, LitInterpolationHandler handler) => handler.Render(writer);
}


