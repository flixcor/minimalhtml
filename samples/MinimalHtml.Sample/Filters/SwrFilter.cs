namespace MinimalHtml.Sample.Filters
{
    public static class SwrFilter
    {
        public static RouteHandlerBuilder WithSwr(this RouteHandlerBuilder builder) => builder.AddEndpointFilter(InvokeAsync);

        private static ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
           context.HttpContext.Response.Headers.Append("x-swr", "true");
           return next(context);
        }
    }
}
