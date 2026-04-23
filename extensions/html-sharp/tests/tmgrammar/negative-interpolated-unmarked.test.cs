// SYNTAX TEST "source.cs" "v2 negative: unmarked interpolated literals stay inert"
// An interpolated literal without the /*lang=html*/ marker MUST NOT carry
// meta.embedded.block.html.cs anywhere — neither in text spans nor in hole
// bodies. This test locks in the invariant so the v2 grammar additions
// (T019+, T043+) cannot regress the "marker is required" contract (FR-002).

class NegativeUnmarkedInterp
{
    // Unmarked $"..." — inert.
    string a = $"<p>{name}</p>";
//                 ^ source.cs - meta.embedded.block.html.cs
//                  ^ source.cs - entity.name.tag.html
//                     ^ source.cs - meta.embedded.block.html.cs

    // Unmarked $@"..." — inert.
    string b = $@"<div>{value}</div>";
//                  ^ source.cs - meta.embedded.block.html.cs
//                   ^ source.cs - entity.name.tag.html

    // Unmarked $"""...""" — inert.
    string c = $"""<span>{x}</span>""";
//                   ^ source.cs - meta.embedded.block.html.cs
//                    ^ source.cs - entity.name.tag.html

    // Unmarked @$"..." — inert (order-swapped prefix).
    string d = @$"<p>{name}</p>";
//                 ^ source.cs - meta.embedded.block.html.cs
//                  ^ source.cs - entity.name.tag.html

    // Unmarked $$"""...""" — inert (N=2).
    string e = $$"""<p>{{name}}</p>""";
//                   ^ source.cs - meta.embedded.block.html.cs
//                    ^ source.cs - entity.name.tag.html

    // Unmarked $$$"""...""" — inert (N=3).
    string f = $$$"""<p>{{{name}}}</p>""";
//                    ^ source.cs - meta.embedded.block.html.cs
//                     ^ source.cs - entity.name.tag.html
}
