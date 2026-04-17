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

        _engine.SetValue("btoa", new Func<string, string>(s =>
        {
            // Treat input as Latin1 bytes
            var bytes = new byte[s.Length];
            for (int i = 0; i < s.Length; i++) bytes[i] = (byte)(s[i] & 0xff);
            return Convert.ToBase64String(bytes);
        }));

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

        class ShimBuffer extends Uint8Array {
            static from(input, encoding) {
                if (typeof input === 'string') {
                    if (encoding === 'binary' || encoding === 'latin1') {
                        const bytes = new Uint8Array(input.length);
                        for (let i = 0; i < input.length; i++) {
                            bytes[i] = input.charCodeAt(i) & 0xff;
                        }
                        return new ShimBuffer(bytes.buffer);
                    }
                    // UTF-8 default
                    const encoded = new TextEncoder().encode(input);
                    return new ShimBuffer(encoded.buffer);
                }
                if (input instanceof Uint8Array) {
                    return new ShimBuffer(input.buffer, input.byteOffset, input.byteLength);
                }
                return new ShimBuffer(input);
            }

            static alloc(size) {
                return new ShimBuffer(size);
            }

            static isBuffer(obj) {
                return obj instanceof ShimBuffer;
            }

            toString(encoding) {
                if (encoding === 'base64') {
                    // Build binary string then base64-encode
                    let binary = '';
                    for (let i = 0; i < this.length; i++) {
                        binary += String.fromCharCode(this[i]);
                    }
                    return btoa(binary);
                }
                if (encoding === 'binary' || encoding === 'latin1') {
                    let s = '';
                    for (let i = 0; i < this.length; i++) {
                        s += String.fromCharCode(this[i]);
                    }
                    return s;
                }
                if (encoding === 'utf8' || encoding === 'utf-8' || !encoding) {
                    return new TextDecoder().decode(this);
                }
                return super.toString();
            }
        }

        export const Buffer = ShimBuffer;
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
