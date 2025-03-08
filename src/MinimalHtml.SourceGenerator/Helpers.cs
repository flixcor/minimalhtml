namespace MinimalHtml.SourceGenerator
{
    internal static class Helpers
    {
        public static string RelativePathToQualifiedClassName(ReadOnlySpan<char> file)
        {
            var remainder = file.Slice(0, file.LastIndexOf('.'));
            var result = new char[remainder.Length];
            for (int i = 0; i < remainder.Length; i++)
            {
                var ch = remainder[i];
                result[i] = ch == '\\' || ch == '/' ? '.' : ch;
            }
            return new string(result).Trim('.');
        }

        public static ReadOnlySpan<char> ToRelative(ReadOnlySpan<char> file, ReadOnlySpan<char> rootDir)
        {
            var indexOfLastFolder = rootDir.Slice(0, rootDir.Length - 1).LastIndexOfAny('\\', '/') + 1;
            return file.Slice(indexOfLastFolder);
        }
    }
}
