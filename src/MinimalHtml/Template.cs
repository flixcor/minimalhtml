using System.IO.Pipelines;

namespace MinimalHtml;

public delegate ValueTask<FlushResult> Template<in T>(PipeWriter writer, T context) where T : allows ref struct;
public delegate ValueTask<FlushResult> Template(PipeWriter writer);
