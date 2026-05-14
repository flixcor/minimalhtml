using System.Collections.Concurrent;
using System.IO.Pipelines;
using Jint.Native;

namespace MinimalHtml.Vite.Lit;

public sealed class LitRuntime(LitSsrInfoResolver resolver) : ILitRuntime
{
    private readonly ConcurrentBag<PoolEntry> _pool = new();

    public static ILitRuntime? Current { get; internal set; }

    public async ValueTask<FlushResult> Render(PipeWriter writer, List<JsValue> literals, List<JsValue> values)
    {
        var options = await resolver.GetOptions();
        var modulePath = Path.Combine(options.ServerPath, options.ServerModule);
        var currentMtime = File.GetLastWriteTimeUtc(modulePath);

        var renderer = _pool.TryTake(out var entry) && entry.ModuleMtime == currentMtime
            ? entry.Renderer
            : new LitRenderer(options);

        try
        {
            return await renderer.Render(writer, literals, values);
        }
        finally
        {
            _pool.Add(new PoolEntry(renderer, currentMtime));
        }
    }

    private readonly record struct PoolEntry(LitRenderer Renderer, DateTime ModuleMtime);
}
