using StronglyTypedIds;

namespace MinimalHtml.SourceGenerator
{
    internal readonly record struct Interpolation : IEquatable<Interpolation>
    {
        public readonly string StringEndToken;
        public readonly EquatableArray<string> Contents;

        public Interpolation(string stringEndToken, EquatableArray<string> contents)
        {
            StringEndToken = stringEndToken;
            Contents = contents;
        }
    }
}
