using System.Buffers;

namespace MinimalHtml.Sample;

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
        TemplateEncoder.Html.WriteEncoded(page, propName);
        page.Write("=\""u8);
        TemplateEncoder.Html.WriteEncoded(page, value, format, null);
        page.Write("\""u8);
        return new();
    };

    public static Template IfTrueish(string propName, string? value) => (page) =>
    {
        if (string.IsNullOrWhiteSpace(value)) return new();
        TemplateEncoder.Html.WriteEncoded(page, propName);
        page.Write("=\""u8);
        TemplateEncoder.Html.WriteEncoded(page, value);
        page.Write("\""u8);
        return new();
    };

    public static Template IfTrueish(string propName, bool? value) => (page) =>
    {
        if (!value.GetValueOrDefault()) return new();
        TemplateEncoder.Html.WriteEncoded(page, propName);
        return new();
    };
}
