namespace MinimalHtml.Sample.Components;

public class NavLink(IHttpContextAccessor acc)
{
    public readonly Template<string> Render = (page, href) => page.Html($"""
        href="{href}" {IfTrueish("aria-current", acc.HttpContext?.Request.Path.StartsWithSegments(href) == true ? "page" : null)}
        """);
}
