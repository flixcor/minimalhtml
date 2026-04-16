using System.IO.Pipelines;
using System.Text.Unicode;
using Jint;
using Jint.Native;

namespace MinimalHtml.Lit;

public class LitRenderer
{
    public static LitRenderer? Default { get; internal set; }

    private readonly Engine _engine;

    public LitRenderer(LitOptions options)
    {
        var serverPath = options.ServerPath;
        var modulePath = Path.Combine(serverPath, options.ServerModule);

        _engine = new Engine(engineOptions =>
        {
            engineOptions.EnableModules(serverPath);
        });
        _engine.Modules.Add("buffer", """
        if (typeof TextEncoder === 'undefined') {
            globalThis.TextEncoder = function TextEncoder() { this.encoding = 'utf-8'; };
            globalThis.TextEncoder.prototype.encode = function(str) {
                var bytes = [];
                for (var i = 0; i < str.length; i++) {
                    var code = str.charCodeAt(i);
                    if (code < 0x80) {
                        bytes.push(code);
                    } else if (code < 0x800) {
                        bytes.push(0xC0 | (code >> 6));
                        bytes.push(0x80 | (code & 0x3F));
                    } else {
                        bytes.push(0xE0 | (code >> 12));
                        bytes.push(0x80 | ((code >> 6) & 0x3F));
                        bytes.push(0x80 | (code & 0x3F));
                    }
                }
                return new Uint8Array(bytes);
            };
        }

        export const Buffer = {
            from(str, encoding) {
                // You don't actually need real Buffer behavior,
                // just enough to not crash if something references it
                return new TextEncoder().encode(str);
            },
            isBuffer(obj) { return false; }
        };
        """);


        _engine.Execute("""
        globalThis.window = globalThis;
        globalThis.customElements = {
            _registry: new Map(),
            define(name, constructor) {
                // Access observedAttributes to trigger Lit's finalize
                void constructor.observedAttributes;
                this._registry.set(name, constructor);
            },
            get(name) {
                return this._registry.get(name);
            }
        };
        """);
        var serverModule = _engine.Modules.Import(modulePath);
        var renderFn = serverModule.Get("renderHtml");
        _engine.SetValue("renderHtml", renderFn);
    }

    public JsArray GetJsArray(int capacity = 0, int initialLength = 0) => new(_engine, (uint)capacity, (uint)initialLength);

    public async ValueTask<FlushResult> Render(PipeWriter writer, JsArray literals, JsArray values)
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
