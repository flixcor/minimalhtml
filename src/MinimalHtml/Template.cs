using System.IO.Pipelines;

namespace MinimalHtml;

public delegate ValueTask<FlushResult> Template<in T>((PipeWriter Writer, CancellationToken Token) tuple, T context);
public delegate ValueTask<FlushResult> Template((PipeWriter Writer, CancellationToken Token) tuple);
