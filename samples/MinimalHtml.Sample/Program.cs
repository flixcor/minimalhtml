using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using MinimalHtml;
using MinimalHtml.AspNetCore;
using MinimalHtml.Sample;
using MinimalHtml.Sample.Components;
using MinimalHtml.Sample.Pages;
using MinimalHtml.Vite;

#if DEBUG
// Use the default builder during inner-loop so Hot Reload works
var builder = WebApplication.CreateBuilder(args);
#else
// Use the slim builder for Release builds
var builder = WebApplication.CreateSlimBuilder(args);
builder.WebHost.UseStaticWebAssets();
#endif

builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    });

builder.Services.AddSingleton<FakeDatabase>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<NavLink>();
builder.Services.RegisterViteAssets(importmapPath: ".vite/importmap.json");
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddResponseCompression();
    builder.Services.AddOutputCache();
}


var app = builder.Build();

MinimalHtml.Sample.Assets.Initialize(app);

if (!app.Environment.IsDevelopment())
{
    app.UseResponseCompression();
    app.UseOutputCache();
    TemplateCache.Cache();
}

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = (ctx) =>
    {
        var request = ctx.Context.Request;
        var response = ctx.Context.Response;
        response.Headers.CacheControl = request.Path.StartsWithSegments("/assets")
            ? "public, max-age=604800, immutable"
            : "no-cache";
    }
});

//app.UseAntiforgery();
Home.Map(app);
StreamingTable.Map(app);
Xss.Map(app);
Lit.Map(app);
Forms.Map(app);
ActiveSearchPage.Map(app);
AnyOrder.Map(app);
StaleWhileRevalidate.Map(app);
CssModules.Map(app);
MinimalHtml.Sample.Api.Version.Map(app);

app.Run();


[JsonSerializable(typeof(JsonObject))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
