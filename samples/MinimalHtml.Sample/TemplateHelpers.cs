namespace MinimalHtml.Sample;

public static class TemplateHelpers
{
    public static Template<string?> IfNotNullOrWhiteSpace(Template<string> template) => 
        (page, value) => string.IsNullOrWhiteSpace(value) 
            ? new() 
            : template(page, value);

    public static Template<string?> IfNotNullOrWhiteSpace(Template<string> template, Template elseTemplate) => 
        (page, value) => string.IsNullOrWhiteSpace(value) 
            ? elseTemplate(page) 
            : template(page, value);
}
