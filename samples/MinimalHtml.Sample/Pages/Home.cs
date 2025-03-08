using MinimalHtml.Sample.Components;
using MinimalHtml.Sample.Filters;
using MinimalHtml.Sample.Layouts;

namespace MinimalHtml.Sample.Pages;

public partial class Home
{
    private static readonly Bogus.DataSets.Lorem s_lorem = new();

    private static readonly TabListItem s_firstTab = new()
    {
        Id = "im-the-first-tab",
        Tab = page => page.Html($"""
            First tab
            """),
        Panel = page => page.Html($"""
            {(Enumerable.Range(0, 2), Paragraphs)}
            """)
    };

    private static readonly TabListItem s_secondTab = new()
    {
        Id = "im-the-second-tab",
        Tab = page => page.Html($"""
            Second tab
            """),
        Panel = page => page.Html($"""
            {(Enumerable.Range(0, 2), Paragraphs)}
            """)
    };

    private static readonly TabListItem s_thirdTab = new()
    {
        Id = "im-the-third-tab",
        Tab = page => page.Html($"""
            Third tab
            """),
        Panel = page => page.Html($"""
            {(Enumerable.Range(0, 2), Paragraphs)}
            """)
    };


    private static readonly TabListItem s_fourthTab = new()
    {
        Id = "im-the-fourth-tab",
        Tab = page => page.Html($"""
            Fourth tab
            """),
        Panel = page => page.Html($"""
            {(Enumerable.Range(0, 2), Paragraphs)}
            """)
    };

    private static readonly TabListItem s_fifthTab = new()
    {
        Id = "im-the-fifth-tab",
        Tab = page => page.Html($"""
            Fifth tab
            """),
        Panel = page => page.Html($"""
            {(Enumerable.Range(0, 2), Paragraphs)}
            """)
    };

    private static Flushed Paragraphs(HtmlWriter page, int _) => page.Html($"""<p>{s_lorem.Paragraphs(4)}</p>""");

    private static Flushed Page(HtmlWriter page) => page.Html($"""
      <h2>Progressive enhancement</h2>
      <p>
          Whithout javascript, these tabs are just links to specific anchors on the page. 
      </p>
      <p>
          When javascript kicks in, only one panel is visible at a time and keyboard navigation works as you would expect with a tablist.
      </p>
      {TabList.Render(s_firstTab, s_secondTab, s_thirdTab)}
      {TabList.Render(s_fourthTab, s_fifthTab)}
      """);

    private static Flushed Head(HtmlWriter page) => page.Html($"{Assets.Script:Components/TabList.js}{Assets.Style:Components/TabList.css}");

    public static void Map(IEndpointRouteBuilder builder) => builder.MapGet("/", static () => Results.Extensions.WithLayout(Page, Head));
}
