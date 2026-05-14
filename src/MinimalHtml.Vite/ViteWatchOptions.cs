using Microsoft.Extensions.Logging;

namespace MinimalHtml.Vite;

public class ViteWatchOptions
{
    /// <summary>
    /// Master switch. Default: true. The hosted service additionally requires
    /// <c>IHostEnvironment.IsDevelopment()</c>, so leaving this at the default
    /// limits the watcher to development.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Directory containing package.json. Default: the host's content root.
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Package-manager executable. Default: auto-detected from the lockfile
    /// in <see cref="WorkingDirectory"/> (pnpm → yarn → bun → npm).
    /// </summary>
    public string? Command { get; set; }

    /// <summary>
    /// Arguments passed to <see cref="Command"/>. Default: <c>run build:watch</c>.
    /// The script name (last token) must exist in package.json or startup fails.
    /// </summary>
    public string Arguments { get; set; } = "run build:watch";

    /// <summary>
    /// Log level for stdout lines from the watcher process. Stderr is always
    /// logged at <see cref="LogLevel.Warning"/>. Default: <see cref="LogLevel.Information"/>.
    /// </summary>
    public LogLevel OutputLogLevel { get; set; } = LogLevel.Information;
}
