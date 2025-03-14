using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Mvc;
using MinimalHtml.Sample.Filters;
using MinimalHtml.Sample.Layouts;
using ZodNet;
using static System.Text.Encoding;

namespace MinimalHtml.Sample.Pages
{
    public class Forms
    {
        public static void Map(IEndpointRouteBuilder builder)
        {
            var group = builder.MapGroup("/forms");

            group.MapGet("/", static () => Results.Extensions
                .WithLayout(static page => Render(page), p => p.Html($"{Assets.Style:/Pages/Forms.css}")))
                .WithEtag()
                .WithSwr()
                .CacheOutput();

            group.MapPost("/", static (FormModel starship) => Results.Extensions.WithLayout(Render, starship, p => p.Html($"{Assets.Style:/Pages/Forms.css}")))
                .DisableAntiforgery()
                .WithMetadata(new RequestSizeLimitAttribute(300_000_000));
        }

        private static Flushed RenderGroup(HtmlWriter page, FormModel.SubGroupsType group) => page.Html($"""
        <fieldset>
            <legend>Recurring group</legend>
            <remove-group-button hidden><button type=button title="Remove Group">Remove Group</button></remove-group-button>
            <label>
                <span>Required text area within group</span>
                <textarea placeholder="Lorem" name="{group.Question.Name}">{group.Question.Value}</textarea>
            </label>
        </fieldset>
        """);

        private static Flushed Render(HtmlWriter page, FormModel? model = null)
        {
            var parsed = model;
            model ??= new();
            return page.Html($"""
            <h2>Forms</h2>
            <form method="POST" enctype="multipart/form-data" action="#results" class="split-with-output">
                <fieldset>
                    <legend>This form uses a source generator for model binding</legend>
                    <label>
                        <span>Multiple select</span>
                        <select name="{model.Options.Name}" multiple>
                            <option value="1" {IfTrueish("selected", model.Options.Contains(1))}>Option 1</option>
                            <option value="2" {IfTrueish("selected", model.Options.Contains(2))}>Option 2</option>
                        </select>
                    </label>
                    <label>
                        <span>Simple number input</span>
                        <input min="0" max="100" type="number" name="{model.Number.Name}" placeholder="0-100" value="{model.Number.Value}">
                    </label>
                    <label>
                        <span>Boolean input</span>
                        <input type="checkbox" name="{model.Boolean.Name}" {IfTrueish("checked", model.Boolean)}>
                    </label>
                    <label>
                        <span>File input</span>
                        <input type="file" name="{model.MyFile.Name}">
                    </label>
                    <label>
                        <span>Multiple files</span>
                        <input type="file" multiple name="{model.MyFiles.Name}">
                    </label>
                    {Assets.Script:/Components/recurring-group.ts}
                    <recurring-groups minlength="1" maxlength="3">
                        {(model.SubGroups.DefaultIfEmpty(), RenderGroup)}
                        <template>
                            {(new FormModel.SubGroupsType(), RenderGroup)}
                        </template>
                        <add-group-button hidden><button type="button">Add Group</button></add-group-button>
                    </recurring-groups>
                    <footer>
                        <button type="reset">Reset</button>
                        <button type="submit">Submit</button>
                    </footer>
                </fieldset>
                {(parsed, RenderParsed)}
            </form>
            
            """);
        }

        private static Flushed RenderParsed(HtmlWriter page, FormModel? parsed) => parsed == null ? default : page.Html($"""
        <fieldset id="results">
        <legend>Strongly typed model</legend>
        <pre class="language-css"><code class="language-css">{JsonSerializer.Serialize(parsed, FormModelSerializerContext.Default.FormModel)}</code></pre>
        </fieldset>
        """);
    }

    [JsonSerializable(typeof(FormModel))]
    [JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    public partial class FormModelSerializerContext: JsonSerializerContext
    {

    }

    public partial class FormModelJsonConverter: JsonConverter<FormModel>
    {

    }

    [JsonConverter(typeof(FormModelJsonConverter))]
    public partial class FormModel : IBindableFromHttpContext<FormModel>
    {
        [Zod]
        private static readonly Expression<Func<IZodRoot, IZodRoot>> s_config = (x) => x
            .Array("Options").OfNumbers().Build()
            .Number("Number").Nullable().Build()
            .Bool("Boolean").Build()
            .File("MyFile").Build()
            .Array("MyFiles").OfFiles().Build()
            .Array("SubGroups")
                .OfObjects()
                    .String("Question")
                        .MinLength(1)
                        .MaxLength(20)
                        .Build()
                    .Build();
    }
}
