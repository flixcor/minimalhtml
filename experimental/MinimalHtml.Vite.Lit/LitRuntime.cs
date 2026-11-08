using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using Jint.Native;

namespace MinimalHtml.Vite.Lit;

public sealed class LitRuntime(LitSsrInfoResolver resolver) : ILitRuntime, IDisposable
{
    private readonly ConcurrentBag<PoolEntry> _pool = new();

    public static ILitRuntime? Current { get; internal set; }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "Renderer ownership transfers to _pool, which disposes everything in Dispose().")]
    public async ValueTask<FlushResult> Render(PipeWriter writer, List<JsValue> literals, List<JsValue> values)
    {
        var options = await resolver.GetOptions();
        var modulePath = Path.Combine(options.ServerPath, options.ServerModule);
        var currentMtime = File.GetLastWriteTimeUtc(modulePath);

        LitRenderer renderer;
        if (_pool.TryTake(out var entry))
        {
            if (entry.ModuleMtime == currentMtime)
            {
                renderer = entry.Renderer;
            }
            else
            {
                entry.Renderer.Dispose();
                renderer = new LitRenderer(options);
            }
        }
        else
        {
            renderer = new LitRenderer(options);
        }

        try
        {
            return await renderer.Render(writer, literals, values);
        }
        finally
        {
            _pool.Add(new PoolEntry(renderer, currentMtime));
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        while (_pool.TryTake(out var entry))
        {
            entry.Renderer.Dispose();
        }
    }

    private readonly record struct PoolEntry(LitRenderer Renderer, DateTime ModuleMtime);
}
