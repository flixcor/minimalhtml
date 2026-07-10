using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MinimalHtml.Vite;

internal sealed class ViteWatcher(
    IHostEnvironment env,
    IOptions<ViteWatchOptions> options,
    IHostApplicationLifetime lifetime,
    ILogger<ViteWatcher> logger) : IHostedService, IAsyncDisposable
{
    private Process? _process;
    private bool _stopping;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var opts = options.Value;
        if (!opts.Enabled || !env.IsDevelopment())
        {
            return Task.CompletedTask;
        }

        var workingDir = ResolveWorkingDirectory(opts, env);
        var command = opts.Command ?? DetectPackageManager(workingDir);
        EnsureScriptExists(workingDir, opts.Arguments);

        var psi = new ProcessStartInfo(command, opts.Arguments)
        {
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        Process? p;
        try
        {
            p = Process.Start(psi);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"vite watch: failed to start '{command} {opts.Arguments}' in '{workingDir}'. " +
                "Set ViteWatchOptions.Command to override the package-manager executable.", ex);
        }

        if (p is null)
        {
            throw new InvalidOperationException(
                $"vite watch: '{command} {opts.Arguments}' in '{workingDir}' returned no process handle.");
        }

        _process = p;
        p.OutputDataReceived += (_, e) =>
        {
            if (e.Data is { Length: > 0 } line) logger.Log(opts.OutputLogLevel, "[vite] {Line}", line);
        };
        p.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is { Length: > 0 } line) logger.LogWarning("[vite] {Line}", line);
        };
        p.EnableRaisingEvents = true;
        p.Exited += (_, _) =>
        {
            if (_stopping) return;
            logger.LogWarning("[vite] watch process exited unexpectedly with code {Code}", p.ExitCode);
        };
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();

        lifetime.ApplicationStopping.Register(StopProcess);

        logger.LogInformation(
            "vite watch started: {Command} {Arguments} (cwd: {Cwd}, pid: {Pid})",
            command, opts.Arguments, workingDir, p.Id);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        StopProcess();
        return Task.CompletedTask;
    }

    private void StopProcess()
    {
        _stopping = true;
        var p = Interlocked.Exchange(ref _process, null);
        if (p is null) return;
        try
        {
            if (!p.HasExited)
            {
                p.Kill(entireProcessTree: true);
                p.WaitForExit(2000);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "vite watch: error stopping child process");
        }
        finally
        {
            p.Dispose();
        }
    }

    public ValueTask DisposeAsync()
    {
        StopProcess();
        return ValueTask.CompletedTask;
    }

    private static string ResolveWorkingDirectory(ViteWatchOptions opts, IHostEnvironment env)
    {
        var raw = opts.WorkingDirectory;
        if (string.IsNullOrWhiteSpace(raw)) return env.ContentRootPath;
        return Path.IsPathRooted(raw) ? raw : Path.GetFullPath(Path.Combine(env.ContentRootPath, raw));
    }

    private static string DetectPackageManager(string workingDir)
    {
        if (File.Exists(Path.Combine(workingDir, "pnpm-lock.yaml"))) return "pnpm";
        if (File.Exists(Path.Combine(workingDir, "yarn.lock"))) return "yarn";
        if (File.Exists(Path.Combine(workingDir, "bun.lockb")) ||
            File.Exists(Path.Combine(workingDir, "bun.lock"))) return "bun";
        return "npm";
    }

    private static void EnsureScriptExists(string workingDir, string arguments)
    {
        var scriptName = ExtractScriptName(arguments);
        if (string.IsNullOrWhiteSpace(scriptName)) return;

        var packageJsonPath = Path.Combine(workingDir, "package.json");
        if (!File.Exists(packageJsonPath))
        {
            throw new InvalidOperationException(
                $"vite watch: package.json not found at '{packageJsonPath}'. " +
                "Set ViteWatchOptions.WorkingDirectory to the folder that contains it.");
        }

        using var doc = JsonDocument.Parse(File.ReadAllBytes(packageJsonPath));
        if (!doc.RootElement.TryGetProperty("scripts", out var scripts) ||
            scripts.ValueKind != JsonValueKind.Object ||
            !scripts.TryGetProperty(scriptName, out _))
        {
            throw new InvalidOperationException(
                $"vite watch: script '{scriptName}' is missing from '{packageJsonPath}'. " +
                "Add it to your package.json, or override ViteWatchOptions.Arguments.");
        }
    }

    private static string ExtractScriptName(string arguments)
    {
        // "run build:watch" → "build:watch"; "build:watch" → "build:watch"
        var parts = arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "";
        return parts.Length >= 2 && string.Equals(parts[0], "run", StringComparison.OrdinalIgnoreCase)
            ? parts[1]
            : parts[^1];
    }
}
