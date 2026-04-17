using System.IO.Pipelines;
using BenchmarkDotNet.Running;

namespace Fluid.Benchmarks
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--loop")
            {
                var b = new MinimalHtmlBenchmarks();
                Console.WriteLine($"Looping MinimalHtml render forever (pid {Environment.ProcessId})");
                while (true)
                {
                    var pipe = new Pipe();
                    await b.Render(pipe.Writer);
                    pipe.Writer.Complete();
                    pipe.Reader.Complete();
                }
            }

            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).RunAllJoined(args: args);
        }
    }
}
