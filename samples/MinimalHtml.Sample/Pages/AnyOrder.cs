using MinimalHtml.Sample.Layouts;

namespace MinimalHtml.Sample.Pages
{
    public class AnyOrder
    {
        public static void Map(IEndpointRouteBuilder builder) => builder
            .MapGet("/any-order", static () => Results.Extensions.WithLayout(Page, Head));

        private static Flushed Head(HtmlWriter page) => page.Html($"{Assets.Style:/Pages/AnyOrder.css}");

        private static Flushed Page(HtmlWriter writer) => writer.Html($$"""
        <h2>Unordered streaming</h2>
        <p>These items resolve in a randomized order, but take their correct spot in the dom with the help of shadow dom and slots</p>
        <any-order>
        <template shadowrootmode="open">
        <style>
            .skeleton {
              cursor: progress;

              background: linear-gradient(
                    to right,
                    var(--surface-1) 20%,
                    var(--surface-2) 30%,
                    var(--surface-3) 70%,
                    var(--surface-4) 80%
                );
                -webkit-background-clip: text;
                background-clip: text;
                -webkit-text-fill-color: transparent;
                text-fill-color: transparent;
                background-size: 500% auto;
                animation: textShine 2s ease-in-out infinite alternate;
            }

            @keyframes textShine {
                0% {
                    background-position: 0% 50%;
                }
                100% {
                    background-position: 100% 50%;
                }
            }
        </style>
        <slot name="1" part="slot1"><span class="skeleton">Loading...</span></slot>
        <slot name="2" part="slot2"><span class="skeleton">Loading...</span></slot>
        <slot name="3" part="slot3"><span class="skeleton">Loading...</span></slot>
        <slot name="4" part="slot4"><span class="skeleton">Loading...</span></slot>
        </any-order>
        </template>
        {{(Each(Delay(1), Delay(2), Delay(3), Delay(4)), Render)}}
        """);

        private static Flushed Render(HtmlWriter page, Delayed delayed) => page.Html($"""<a href="#" slot="{delayed.Index}">Took {delayed.Delay} ms</a>""");

        readonly record struct Delayed(int Index, int Delay);

        private static int RandomDelay() => Random.Shared.Next(100, 5000);

        private static async Task<Delayed> Delay(int index)
        {
            var delay = RandomDelay();
            await Task.Delay(delay);
            return new(index, delay);
        }

        private static async IAsyncEnumerable<T> Each<T>(params IReadOnlyList<Task<T>> values)
        {
            await foreach (var item in Task.WhenEach(values))
            {
                yield return item.Result;
            }
        }
    }
}
