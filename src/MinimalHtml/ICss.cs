using System.Buffers;

namespace MinimalHtml
{
    public interface ICss
    {
        public static abstract string Path { get; }
            
        public static abstract string Hash();

        public static abstract ValueTask<bool> Link(HtmlWriter page, CancellationToken token);

        public static abstract void RenderBrotli(IBufferWriter<byte> writer);

        public static abstract void RenderUncompressed(IBufferWriter<byte> writer);
    }
}
