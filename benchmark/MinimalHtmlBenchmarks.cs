using System.Buffers;
using System.IO.Pipelines;
using BenchmarkDotNet.Attributes;
using MinimalHtml;

namespace Fluid.Benchmarks
{
    [MemoryDiagnoser]
    public class MinimalHtmlBenchmarks : BaseBenchmarks
    {
        private Pipe _pipe = null!;
        private static readonly MinimalHtml.Template<Product> s_singleProductTemplate = (writer, product) => writer.Html($$"""
                  <li>
                    <h2>{{ product.Name }}</h2>
                         Only {{ product.Price }}
                         {{ (Truncate, (product.Description, 15)) }}
                  </li>
              """);
        private static readonly MinimalHtml.Template<List<Product>> s_htmlTemplate = (writer, products) => writer.Html($$"""
              <ul id='products'>
              {{(products, s_singleProductTemplate)}}
              </ul>
              """);

        public MinimalHtmlBenchmarks()
        {
            CheckBenchmark();
        }


        public override async ValueTask Render(PipeWriter output)
        {
            await s_htmlTemplate(output, Products);
        }

        [IterationSetup]
        public void IterationSetup() => _pipe = new Pipe();

        [IterationCleanup]
        public void IterationCleanup()
        {
            _pipe.Writer.Complete();
            _pipe.Reader.Complete();
        }

        [Benchmark]
        public ValueTask Render() => Render(_pipe.Writer);

        private static ValueTask<FlushResult> Truncate(PipeWriter writer, (string Str, int Length) props)
        {
            var ellipsis = "..."u8;
            if (props.Str.Length <= props.Length)
            {
               TemplateEncoder.Html.WriteEncoded(writer, props.Str);
            }
            else
            {
                var length = Math.Max(0, props.Length - ellipsis.Length);
                TemplateEncoder.Html.WriteEncoded(writer, props.Str.AsSpan(0, length));
                writer.Write(ellipsis);
            }

            return new();
        }
    }
}
