using System.IO.Pipelines;

namespace MinimalHtml;

/// <summary>
/// A template for rendering HTML. A template is a delegate that takes a <see cref="PipeWriter"/> and a context object, and writes HTML to the <see cref="PipeWriter"/>. The template can be asynchronous, allowing for streaming of data as it becomes available. The context object can be used to pass data to the template, allowing for dynamic generation of HTML based on the context.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="writer"></param>
/// <param name="context"></param>
/// <returns></returns>
public delegate ValueTask<FlushResult> Template<in T>(PipeWriter writer, T context) where T : allows ref struct;

/// <summary>
/// A template for rendering HTML. A template is a delegate that takes a <see cref="PipeWriter"/> and writes HTML to the <see cref="PipeWriter"/>. The template can be asynchronous, allowing for streaming of data as it becomes available.
/// </summary>
/// <param name="writer"></param>
/// <returns></returns>
public delegate ValueTask<FlushResult> Template(PipeWriter writer);
