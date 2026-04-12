using System.IO.Pipelines;

namespace MinimalHtml;

public delegate ValueTask<FlushResult> Template<in T>(PipeWriter writer, T context);
public delegate ValueTask<FlushResult> Template(PipeWriter writer);
