namespace MinimalHtml.Sample.Components;

public partial class TabList
{
    public static (IReadOnlyList<TabListItem>, Template<IReadOnlyList<TabListItem>>) Render(
        params IReadOnlyList<TabListItem> items) =>
        (items, Render);

    public static Flushed Render(HtmlWriter page, params IReadOnlyList<TabListItem> items) => page.Html($$"""
          <tab-list>
            <style>
            @scope {
            timeline-scope: {{string.Join(", ", items.Select((x, i) => $"--panel-{i + 1}"))}};
            {{(items.Index(), ScopedStyles)}}
            }
            </style>
              <nav>
                  {{(items, Tab)}}
              </nav>
              {{(items, Panel)}}
          </tab-list>
          """);

    private static Flushed Tab(HtmlWriter page, TabListItem x) => page.Html($"""
        <a href="#panel_{x.Id}" id="{x.Id}">{x.Tab}</a>
        """);

    private static Flushed Panel(HtmlWriter page, TabListItem x) => page.Html($"""
        <section id="panel_{x.Id}" aria-labelledby="{x.Id}">{x.Panel}</section>
        """);

    private static Flushed ScopedStyles(HtmlWriter page, (int Index, TabListItem) x) => page.Html( /*language=css*/$$"""
          section:nth-of-type({{x.Index + 1}}) {
              view-timeline-name: --panel-{{x.Index + 1}};
          }

          a:nth-child({{x.Index + 1}}) {
              animation-timeline: --panel-{{x.Index + 1}};
          }
          """);
}

public class TabListItem
{
    public required string Id { get; init; }
    public required Template Tab { get; init; }
    public required Template Panel { get; init; }
}
