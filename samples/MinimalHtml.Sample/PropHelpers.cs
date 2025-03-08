namespace MinimalHtml
{
    public static class PropHelper
    {
        public static Template IfNotNull<V>(string propName, V? value, string? format = null) where V : struct, IUtf8SpanFormattable => (page) =>
        {
            if (value == null) return new();
            return IfNotNull(propName, value.Value, format)(page);
        };

        public static Template IfNotNull<V>(string propName, V? value, string? format = null) where V : IUtf8SpanFormattable => (page) =>
        {
            if (value == null) return new();
            Helpers.WriteHtmlEscaped(page.Writer, propName);
            Write("=\""u8, page);
            Helpers.WriteFormatted(value, page.Writer, format);
            Write("\""u8, page);
            return new();
        };

        public static Template IfTrueish(string propName, string? value) => (page) =>
        {
            if (string.IsNullOrWhiteSpace(value)) return new();
            Helpers.WriteHtmlEscaped(page.Writer, propName);
            Write("=\""u8, page);
            Helpers.WriteHtmlEscaped(page.Writer, value);
            Write("\""u8, page);
            return new();
        };

        public static Template IfTrueish(string propName, bool? value) => (page) =>
        {
            if (!value.GetValueOrDefault()) return new();
            Helpers.WriteHtmlEscaped(page.Writer, propName);
            return new();
        };

        private static void Write(ReadOnlySpan<byte> input, HtmlWriter output)
        {
            input.CopyTo(output.Writer.GetSpan(input.Length));
            output.Writer.Advance(input.Length);
        }
    }
}
