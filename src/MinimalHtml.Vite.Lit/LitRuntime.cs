using System.Collections.Concurrent;
using System.IO.Pipelines;
using Jint.Native;

namespace MinimalHtml.Vite.Lit;

public sealed class LitRuntime(LitSsrInfoResolver resolver) : ILitRuntime
{
    private readonly ConcurrentBag<LitRenderer> _pool = new();

    public static ILitRuntime? Current { get; internal set; }

    public async ValueTask<FlushResult> Render(PipeWriter writer, List<JsValue> literals, List<JsValue> values)
    {
        var renderer = _pool.TryTake(out var r) ? r : new LitRenderer(await resolver.GetOptions());
        try
        {
            return await renderer.Render(writer, literals, values);
        }
        finally
        {
            _pool.Add(renderer);
        }
    }
}
