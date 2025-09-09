using System.IO.Pipelines;
using System.Text;

namespace MinimalHtml.Test
{
    public class TemplateTests
    {
        private static readonly Template<string> s_helloTemplate = static (writer, str) => writer.Html($"Hello {str}");

        private static async Task<string> GetWorldAsync()
        {
            await Task.Delay(10);
            return "world";
        }

        [Fact]
        public async Task TestWithAsyncProps()
        {
            var result = await RenderToString(static writer => writer.Html($"There is an async template after this: {(GetWorldAsync, s_helloTemplate)}"));
            Assert.Equal(["There is an async template after this: ", "Hello world"], result);
        }

        private static async Task<IReadOnlyList<string>> RenderToString(Template template)
        {
            var pipe = new Pipe();
            var readTask = Task.Run(async () =>
            {
                var result = new List<string>();
                while (true)
                {
                    var readResult = await pipe.Reader.ReadAsync();
                    var str = Encoding.UTF8.GetString(readResult.Buffer);
                    result.Add(str);
                    pipe.Reader.AdvanceTo(readResult.Buffer.End);
                    if (readResult.IsCompleted) break;
                }
                return result;
            });
            var writeTask = Task.Run(async () =>
            {
                await template((pipe.Writer, CancellationToken.None));
                await pipe.Writer.FlushAsync();
                await pipe.Writer.CompleteAsync();
            });
            await writeTask;
            return await readTask;
        }
    }
}
