using System.IO.Pipelines;
using MinimalHtml.Sample.Filters;
using MinimalHtml.Sample.Layouts;

namespace MinimalHtml.Sample.Pages
{
    public class FakeDatabase
    {
        public async IAsyncEnumerable<SampleColumnRowData> GetRows()
        {
            await Task.Delay(1000);
            yield return new("Academic Senate Meeting", new DateOnly(2205, 5, 25), "Building 99 Room 1");
            yield return new("Commencement Meeting", new DateOnly(2205, 12, 15), "Building 42 Room 10");
            await Task.Delay(1000);
            yield return new("Dean's Council", new DateOnly(2206, 2, 1), "Building 35 Room 5");
            await Task.Delay(1000);
            yield return new("Committee on Committees", new DateOnly(2206, 3, 3), "Building 1 Room 201");
        }
    }

    public record SampleColumnRowData(string Description, DateOnly Date, string Location);

    public static class StreamingTable
    {
        private static Flushed Row(HtmlWriter page, SampleColumnRowData data) => page.Html($"""
            <tr>
                <td>{data.Description}</td>
                <td>{data.Date}</td>
                <td>{data.Location}</td>
            </tr>
            """);

        private static Flushed Page(HtmlWriter page, IAsyncEnumerable<SampleColumnRowData> rows) => page.Html($"""
             <h2>Streaming</h2>
             <table>
                 <caption>These rows are streamed incrementally, no javascript needed!</caption>
                 <thead>
                     <tr>
                         <th>Description</th>
                         <th>Date</th>
                         <th>Location</th>
                     </tr>
                 </thead>
                 <tbody>
                     {(rows, Row)}
                 </tbody>
             </table>
             """);
        
        
        public static void Map(IEndpointRouteBuilder builder) => builder.MapGet("/streaming", static (FakeDatabase db) => 
            Results.Extensions.WithLayout(Page, db.GetRows()))
            .WithSwr();
    }
}
