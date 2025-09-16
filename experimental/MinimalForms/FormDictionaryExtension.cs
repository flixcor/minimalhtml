using System.IO.Pipelines;

namespace MinimalForms
{
    public static class FormDictionaryExtension
    {
        public static Task<FormDictionary> GetFormDictionary(this PipeReader pipeReader, string? contentType, long? length, CancellationToken token) => 
            Boundary.TryParseFromContentType(contentType, out var boundary)
                ? pipeReader.GetMultipartFormDicationaryAsync(boundary, length, token)
                : pipeReader.GetUrlEncodedFormDictionaryAsync(token);
    }
}
