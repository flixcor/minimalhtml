using System.IO.Pipelines;
using Jint.Native;

namespace MinimalHtml.Vite.Lit;

public interface ILitRuntime
{
    ValueTask<FlushResult> Render(PipeWriter writer, List<JsValue> literals, List<JsValue> values);
}
