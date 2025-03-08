

using System.Buffers;
using System.IO.Hashing;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.ObjectPool;

namespace MinimalHtml.Sample.Filters
{
    public static class EtagFilter
    {
        public static RouteHandlerBuilder WithEtag(this RouteHandlerBuilder builder) => builder.AddEndpointFilter(InvokeAsync);

        private static async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var result = await next(context);
            return result is IResult inner
                ? new EtagResult(inner)
                : result;
        }

        private class EtagResult(IResult inner) : IResult
        {
            public async Task ExecuteAsync(HttpContext httpContext)
            {
                var feature = httpContext.Features.Get<IHttpResponseBodyFeature>();
                if (feature != null)
                {
                    var logger = httpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("hoi");
                    var instance = new EtagFeature(feature);
                    httpContext.Features.Set<IHttpResponseBodyFeature>(instance);
                    await inner.ExecuteAsync(httpContext);
                    instance.XxHash3Writer.Complete();
                    httpContext.Response.Headers.ETag = instance.XxHash3Writer.Etag;
                    logger.LogInformation(instance.XxHash3Writer.Etag);
                    httpContext.Response.Headers.CacheControl = "no-cache, private";
                    httpContext.Response.Headers.Append("x-swr-etag", instance.XxHash3Writer.Etag);
                    await feature.Writer.FlushAsync(httpContext.RequestAborted);
                    httpContext.Features.Set(feature);
                }

            }
        }

        private class EtagFeature(IHttpResponseBodyFeature inner) : IHttpResponseBodyFeature
        {
            public XxHash3Writer XxHash3Writer { get; } = new(inner.Writer);
            public Stream Stream => inner.Stream;

            public PipeWriter Writer => XxHash3Writer;

            public Task CompleteAsync()
            {
                return Task.CompletedTask;
            }

            public void DisableBuffering() => inner.DisableBuffering();

            public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = default)
             => inner.SendFileAsync(path, offset, count, cancellationToken);

            public Task StartAsync(CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }

        private class XxHash3Writer(PipeWriter inner) : PipeWriter
        {
            private static readonly ArrayPool<byte> s_pool = ArrayPool<byte>.Shared;
            private static readonly ObjectPool<XxHash3> s_objectPool = new DefaultObjectPoolProvider().Create<XxHash3>();

            private byte[] _rented = s_pool.Rent(4096);
            private int _index;
            private readonly XxHash3 _xxHash3 = s_objectPool.Get();
            public string Etag { get; private set; } = "";

            public override void Advance(int bytes)
            {
                _index += bytes;
            }

            public override void CancelPendingFlush()
            {
            }

            public override void Complete(Exception? exception = null)
            {
                var span = _rented.AsSpan().Slice(0, _index);
                _xxHash3.Append(span);
                inner.Write(span);
                s_pool.Return(_rented);
                Etag = GetEtag();
                _xxHash3.Reset();
                s_objectPool.Return(_xxHash3);
            }

            public override Flushed FlushAsync(CancellationToken cancellationToken = default)
            {
                return default;
            }

            private void Grow(int sizeHint)
            {
                if (sizeHint + _index >= _rented.Length)
                {
                    var span = _rented.AsSpan().Slice(0, _index);
                    _xxHash3.Append(span);
                    inner.Write(span);
                    s_pool.Return(_rented);
                    _rented = s_pool.Rent(int.Max(sizeHint, 4096));
                    _index = 0;
                }
            }

            public override Memory<byte> GetMemory(int sizeHint = 0)
            {
                Grow(sizeHint);
                return _rented.AsMemory().Slice(_index);
            }

            public override Span<byte> GetSpan(int sizeHint = 0)
            {
                Grow(sizeHint);
                return _rented.AsSpan().Slice(_index);
            }

            private string GetEtag()
            {
                const int HashSizeInBytes = 8;
                const int HashSizeInChars = 12;
                Span<byte> hashBytes = stackalloc byte[HashSizeInBytes];
                Span<char> chars = stackalloc char[HashSizeInChars];
                _xxHash3.GetCurrentHash(hashBytes);
                Convert.TryToBase64Chars(hashBytes, chars, out _);
                return $"\"{chars}\"";
            }
        }
    }
}
