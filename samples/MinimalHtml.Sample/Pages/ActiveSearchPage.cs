using Microsoft.AspNetCore.Mvc;
using MinimalHtml.Sample.Layouts;

namespace MinimalHtml.Sample.Pages
{

    public class ActiveSearchPage
    {
        private static readonly SearchResult[] s_db = [
            new("I am result 1"),
            new("I am result 2"),
            new("I am result 3"),
            new("I am result 4"),
        ];

        private static async IAsyncEnumerable<SearchResult> Db()
        {
            yield return s_db[0];
            await Task.Delay(50);
            yield return s_db[1];
            yield return s_db[2];
            await Task.Delay(50);
            yield return s_db[3];
        }

        private static IAsyncEnumerable<SearchResult> QuerySearchResults(string query)
        {
            return Db()
                .Where(x => x.Text.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        public static void Map(IEndpointRouteBuilder builder) => builder.MapGet("/active-search", (
            [FromHeader(Name = "Sec-Fetch-Dest")] string? fetchDest,
            [FromQuery] string? query) =>
            fetchDest == "document" || query == null
                ? Results.Extensions.WithLayout(Page, query)
                : Results.Extensions.Html(RenderResults, query));

        private static Flushed RenderSearchResult(HtmlWriter page, SearchResult result) => page.Html($"""
             <tr>
                 <td>{result.Text}</td>
             </tr>
             """);

        private static Flushed Empty(HtmlWriter page) => page.Html($"""<div id="results"></div>""");

        private static Flushed RenderResults(HtmlWriter page, string query) => page.Html($"""
            <table id="results">
                <caption>Search results</caption>
                <thead>
                    <tr>
                        <th>Result</th>
                    </tr>
                </thead>
                <tbody>
                    {(QuerySearchResults(query), RenderSearchResult)}
                </tbody>
            </table>
            """);

        private static Flushed Page(HtmlWriter page, string? query) => page.Html($"""
             <h2>Active search</h2>
             {Assets.Script:Components/ActiveSearch.js}
             <active-search>
                  <form data-target="#results" data-debounce="500">
                      <fieldset>
                      <label>Search<input name="query" required></label>
                      <button>Submit</button>
                      </fieldset>
                  </form>
              </active-search>
              {(query, (HtmlWriter p, string? q) => !string.IsNullOrWhiteSpace(q)
                    ? RenderResults(p, q)
                    : Empty(page))}
             """);


        private record SearchResult(string Text);
    }
}
