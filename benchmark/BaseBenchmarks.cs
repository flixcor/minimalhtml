using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using MinimalHtml;

namespace Fluid.Benchmarks
{
    public static partial class TemplateCache
    {
        [FillTemplateCache]
        public static partial void Cache();
    }
    
    public abstract class BaseBenchmarks
    {
        protected readonly List<Product> Products = new(ProductCount);

        private const int ProductCount = 100;

        private const string Lorem = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum";

        protected readonly static string ProductTemplate;

        static BaseBenchmarks()
        {
            TemplateCache.Cache();
            var assembly = typeof(BaseBenchmarks).Assembly;

            using (var stream = assembly.GetManifestResourceStream("MinimalHtml.Benchmarks.product.liquid"))
            {
                using var streamReader = new StreamReader(stream);
                ProductTemplate = streamReader.ReadToEnd();
            }
        }

        public BaseBenchmarks()
        {
            for (int i = 0; i < ProductCount; i++)
            {
                var product = new Product("Name" + i, i, Lorem);
                Products.Add(product);
            }
        }

        public async Task CheckBenchmark()
        {
            var pipe = new Pipe();
            await Render(pipe.Writer);
            await pipe.Writer.CompleteAsync();
            var bytes = await pipe.Reader.ReadAsync();
            var result = Encoding.UTF8.GetString(bytes.Buffer);
            if (string.IsNullOrEmpty(result) ||
                !result.Contains("<h2>Name0</h2>") ||
                !result.Contains($"<h2>Name{ProductCount - 1}</h2>") ||
                !result.Contains($"Lorem ipsum ...") ||
                !result.Contains($"Only 0") ||
                !result.Contains($"Only {ProductCount - 1}"))
            {
                throw new InvalidOperationException($"Template rendering failed: \n {result}");
            }
        }
        public abstract ValueTask Render(PipeWriter writer);
    }
}
