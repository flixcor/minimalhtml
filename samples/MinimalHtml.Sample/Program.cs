global using static MinimalHtml.PropHelper;
using MinimalHtml.Sample.Pages;
using MinimalHtml.Sample;
using MinimalHtml.Sample.Components;

#if DEBUG
// Use the default builder during inner-loop so Hot Reload works
var builder = WebApplication.CreateBuilder(args);
#else
// Use the slim builder for Release builds
var builder = WebApplication.CreateSlimBuilder(args);
builder.WebHost.UseStaticWebAssets();
#endif
builder.Services.AddSingleton<FakeDatabase>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<NavLink>();

if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddResponseCompression();
    builder.Services.AddOutputCache();
}


var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseResponseCompression();
    app.UseOutputCache();
    TemplateCache.Cache();
}


if (!app.Environment.IsDevelopment())
{
    AssetResolver.Setup(app);
}
app.MapStaticAssets();
//app.UseAntiforgery();
Home.Map(app);
StreamingTable.Map(app);
Xss.Map(app);
Lit.Map(app);
Forms.Map(app);
ActiveSearchPage.Map(app);
AnyOrder.Map(app);
StaleWhileRevalidate.Map(app);

app.Run();
