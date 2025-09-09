using System.Reflection;
using System.Text.Json.Nodes;

namespace MinimalHtml.Sample.Api
{
    public class Version
    {
        private static readonly string? s_version = Assembly
            .GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            .Split('+')
            ?.LastOrDefault()
            ?.Substring(0, 7);
        
        public static void Map(IEndpointRouteBuilder builder) => builder.MapGet("/api/version-number", 
            () => TypedResults.Ok(new JsonObject{["version"] = s_version}));
    }
}
