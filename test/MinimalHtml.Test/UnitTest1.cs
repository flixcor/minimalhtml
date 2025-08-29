using System.IO.Pipelines;

namespace MinimalHtml.Test
{
    public class UnitTest1
    {
        static readonly Template<string> s_helloTemplate = static (writer, str) => writer.Html($"Hello {str}");

        static async Task<string> GetWorldAsync()
        {
            await Task.Delay(1000);
            return "world";
        }

        [Fact]
        public async Task TestWithAsyncProps()
        {
            var result = await RenderToString(static writer => writer.Html($"{(GetWorldAsync(), s_helloTemplate)}"));
            Assert.Equal("Hello world", result);
        }

        private static async Task<string> RenderToString(Template template)
        {
            var pipe = new Pipe();
            var result = await template((pipe.Writer, CancellationToken.None));
            pipe.Writer.Complete();
            using var streamReader = new StreamReader(pipe.Reader.AsStream());
            return await streamReader.ReadToEndAsync();
        }
    }
}
