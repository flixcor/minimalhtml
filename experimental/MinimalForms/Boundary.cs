

namespace MinimalForms
{
    internal readonly ref struct Boundary
    {
        private readonly ReadOnlySpan<char> _bytes;

        private Boundary(ReadOnlySpan<char> bytes)
        {
            _bytes = bytes;
        }

        public static implicit operator ReadOnlySpan<char>(Boundary boundary) => boundary._bytes; 

        public static bool TryParseFromContentType(string? contentType, out Boundary boundary)
        {
            boundary = default;
            if(contentType == null) return false;
            var span = contentType.AsSpan();
            const string multipart = "multipart/form-data";
            const string boundaryString = "boundary=";
            
            var multipartIndex = span.IndexOf(multipart, StringComparison.OrdinalIgnoreCase);
            if(multipartIndex == -1) return false;
            span = span.Slice(multipartIndex + multipart.Length);
            
            var boundaryStartIndex = span.IndexOf(boundaryString);
            if(boundaryStartIndex == -1) return false;
            span = span.Slice(boundaryStartIndex + boundaryString.Length);

            var boundaryEndIndex = span.IndexOf(';');
            if(boundaryEndIndex != -1)
            {
                span = span.Slice(0, boundaryEndIndex);
            }
            span = span.Trim('"');
            boundary = new(span);
            return true;
        }
    }
}
