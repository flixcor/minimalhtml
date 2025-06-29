using System.IO.Pipelines;

namespace MinimalHtml;

public delegate ValueTask<FlushResult> Template<in T>(PipeWriter page, T context, CancellationToken token);
public delegate ValueTask<FlushResult> Template(PipeWriter page, CancellationToken token);
