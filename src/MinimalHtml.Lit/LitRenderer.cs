using System.IO.Pipelines;
using System.Text.Unicode;
using Jint;
using Jint.Native;

namespace MinimalHtml.Lit;

public class LitRenderer
{
    public static void Setup(LitOptions options)
    {
        Default = options;
    }
    public static LitOptions? Default { get; internal set; }

    private readonly Engine _engine;

    public LitRenderer(LitOptions options)
    {
        var serverPath = options.ServerPath;
        var modulePath = Path.Combine(serverPath, options.ServerModule);

        _engine = new Engine(engineOptions =>
        {
            engineOptions.EnableModules(serverPath);
        });

        _engine.Modules.Add("buffer", JintShims.BufferModule);

        var serverModule = _engine.Modules.Import(modulePath);
        var renderFn = serverModule.Get("renderHtml");
        _engine.SetValue("renderHtml", renderFn);
    }

    public async ValueTask<FlushResult> Render(PipeWriter writer, List<JsValue> literals, List<JsValue> values)
    {
        var write = JsValue.FromObject(_engine, new Action<string>(chunk =>
        {
            var span = writer.GetSpan(chunk.Length);
            var written = 0;
            while (Utf8.FromUtf16(chunk, span, out var read, out written) == System.Buffers.OperationStatus.DestinationTooSmall)
            {
                writer.Advance(written);
                chunk = chunk[read..];
                span = writer.GetSpan(chunk.Length);
            }
            writer.Advance(written);
        }));
        var flush = JsValue.FromObject(_engine, new Func<Task>(async () =>
        {
            await writer.FlushAsync();
        }));
        await _engine.InvokeAsync("renderHtml", literals, values, write, flush);
        return new();
    }
}
