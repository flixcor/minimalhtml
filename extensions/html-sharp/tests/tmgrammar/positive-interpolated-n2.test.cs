// SYNTAX TEST "source.cs" "v2 positive: raw interpolated N=2 text spans + hole bodies"
// Mirrors tests/fixtures/positive/interpolated-raw-n2.cs Case 1.
// At N=2, the hole opener is EXACTLY `{{` and the closer EXACTLY `}}`.
// Per contracts/grammar.md §2.3 / §3: text spans carry meta.embedded.block.html.cs
// and HTML tag scopes; hole bodies carry meta.interpolation.cs.

class InterpolatedRawN2
{
    // Case 1: single `{{name}}` hole in element-body context.
    string a = /*lang=html*/$$"""<p>{{name}}</p>""";
//                               ^^^ meta.embedded.block.html.cs
//                                ^ entity.name.tag.html
//                                     ^^^^ meta.interpolation.cs
//                                           ^^^^ meta.embedded.block.html.cs
}
