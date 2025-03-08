namespace MinimalHtml.Sample.Components
{
    public class NavLink(IHttpContextAccessor acc)
    {
        public Flushed Render(HtmlWriter page, string href) => page.Html($"""
            href="{href}" {IfTrueish("aria-current", acc.HttpContext?.Request.Path.StartsWithSegments(href) == true ? "page" : null)}
            """);
    }
}
