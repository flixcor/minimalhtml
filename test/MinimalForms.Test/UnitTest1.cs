using System.Buffers;
using System.IO.Pipelines;
using System.Text;

namespace MinimalForms.Test
{
    public class UnitTest1
    {
        [Fact]
        public async Task SingleBuffer()
        {
            var pipe = new Pipe();
            var dictionaryTask = Task.Run(() => pipe.Reader.GetFormDictionary("multipart/form-data; boundary=---------------------------9051914041544843365972754266", null, CancellationToken.None));
            pipe.Writer.Write(MultipartBody());
            await pipe.Writer.CompleteAsync();
            using var dictionary = await dictionaryTask;
            Asserts(dictionary);
        }

        [Fact]
        public async Task MaximumBuffers()
        {
            var pipe = new Pipe();
            var dictionaryTask = Task.Run(() => pipe.Reader.GetFormDictionary("multipart/form-data; boundary=---------------------------9051914041544843365972754266", null,CancellationToken.None));
            var buffer = MultipartBody().ToArray().AsMemory();

            while (!buffer.IsEmpty)
            {
                var mem = pipe.Writer.GetMemory(4096);
                buffer[..1].CopyTo(mem);
                pipe.Writer.Advance(1);
                buffer = buffer[1..];
                await pipe.Writer.FlushAsync();
                await Task.Delay(1);
            }

            await pipe.Writer.CompleteAsync();
            using var dictionary = await dictionaryTask;
            Asserts(dictionary);
        }

        private static ReadOnlySpan<byte> MultipartBody() => """
            -----------------------------9051914041544843365972754266
            Content-Disposition: form-data; name="text"

            text default
            spanning two lines
            -----------------------------9051914041544843365972754266
            Content-Disposition: form-data; name="empty"
            -----------------------------9051914041544843365972754266
            Content-Disposition: form-data; name="file1"; filename="a.txt"
            Content-Type: text/plain

            Content of a.txt.

            -----------------------------9051914041544843365972754266
            Content-Disposition: form-data; name="file2"; filename="a.html"
            Content-Type: text/html

            <!DOCTYPE html><title>Content of a.html.</title>

            -----------------------------9051914041544843365972754266--
            """u8;

        private static void Asserts(FormDictionary dictionary)
        {
            Assert.NotEqual(0, dictionary.Count);

            Assert.False(dictionary.TryGetValue("empty"u8, out _));
            Assert.True(dictionary.TryGetValue("text"u8, out var textRange));
            var text = Assert.Single(textRange);
            Assert.Equal("""
                text default
                spanning two lines
                """u8, text.Span);
            AssertFile(dictionary, "file1"u8, "a.txt", "text/plain", """
            Content of a.txt.
            
            """);
            AssertFile(dictionary, "file2"u8, "a.html", "text/html", """
            <!DOCTYPE html><title>Content of a.html.</title>
            
            """);
        }

        private static void AssertFile(FormDictionary dictionary, ReadOnlySpan<byte> key, string expectedFileName, string expectedContentType, string expectedContent)
        {
            Assert.True(dictionary.TryGetFile(key, out var formFile));
            var file1 = Assert.Single(formFile);
            var contentType = Encoding.UTF8.GetString(file1.ContentType.Span);
            var fileName = Encoding.UTF8.GetString(file1.FileName.Span);
            Assert.Equal(expectedContentType, contentType);
            Assert.Equal(expectedFileName, fileName);
            using var file1Stream = file1.OpenReadStream();
            using var file1Reader = new StreamReader(file1Stream);
            var file1Content = file1Reader.ReadToEnd();
            Assert.Equal(expectedContent, file1Content);
        }
    }
}
