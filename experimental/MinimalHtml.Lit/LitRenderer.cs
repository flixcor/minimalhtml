using System.IO.Pipelines;
using System.Text.Unicode;
using Jint;
using Jint.Native;

namespace MinimalHtml.Lit;

public class LitRenderer
{
    private readonly Engine _engine = PrepareEngine();
    private readonly JsArray _literals;
    private readonly JsArray _values;

    public LitRenderer(int literalCount, int valueCount)
    {
        _literals = new JsArray(_engine, (uint)literalCount);
        _values = new JsArray(_engine, (uint)valueCount);
    }

    private static Engine PrepareEngine()
    {
        var engine = new Engine();
        engine.Execute("""
        import { render } from '@lit-labs/ssr';
        import { html } from 'lit';
        globalThis.window = globalThis;
        globalThis.customElements = {
            _registry: new Map(),
            define(name, constructor) {
                this._registry.set(name, constructor);
            },
            get(name) {
                return this._registry.get(name);
            }
        };

        async function renderHtml(strings, values, write, flush) {
            const iterator = render(html(strings, ...values));
            for (const chunk of iterator) {
                if(typeof chunk === 'string') {
                    write(chunk);
                } else {
                    await flush();
                    write(await chunk);
                }
            }
        }
        """);
        return engine;
    }

    public void AddLiteral(string value)
    {
        _literals.Push(value);
    }

    public void AddValue(JsValue value)
    {
        _values.Push(value);
    }

    public string Render(PipeWriter writer)
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
        JsValue.FromObject(_engine, write);
        var renderFunc = _engine.GetValue("renderHtml");
        return renderFunc.Call(_literals, _values, write, flush).AsString();
    }
}
