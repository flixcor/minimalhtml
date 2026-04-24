using System.IO.Pipelines;
using BenchmarkDotNet.Attributes;

namespace Fluid.Benchmarks
{
    [MemoryDiagnoser]
    public class FluidBenchmarks : BaseBenchmarks
    {
        private readonly TemplateOptions _options = new TemplateOptions();
        private readonly FluidParser _parser  = new FluidParser();
        private readonly IFluidTemplate _fluidTemplate;
        private readonly FluidParser _compiledParser = new FluidParser().Compile();
        private MemoryStream _buffer = null!;
        private StreamWriter _writer = null!;

        public FluidBenchmarks()
        {
            _options.MemberAccessStrategy.MemberNameStrategy = MemberNameStrategies.CamelCase;
            _options.MemberAccessStrategy.Register<Product>();
            _parser.TryParse(ProductTemplate, out _fluidTemplate, out var _);

            CheckBenchmark();
        }


        public override async ValueTask Render(PipeWriter pipeWriter)
        {
            var context = new TemplateContext(_options).SetValue("products", Products);
            await using var stream = pipeWriter.AsStream();
            await using var writer = new StreamWriter(stream);
            await _fluidTemplate.RenderAsync(writer, context);
        }

        [IterationSetup]
        public void IterationSetup()
        {
            _buffer = new MemoryStream(capacity: 65536);
            _writer = new StreamWriter(_buffer);
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            _writer.Dispose();
            _buffer.Dispose();
        }

        [Benchmark]
        public async ValueTask Render()
        {
            var context = new TemplateContext(_options).SetValue("products", Products);
            await _fluidTemplate.RenderAsync(_writer, context);
        }
    }
}
