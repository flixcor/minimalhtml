// SYNTAX TEST "source.cs" "v2 negative: raw interpolated N>=4 stays inert (FR-011)"
// A raw interpolated literal with 4 or more leading `$` is NEVER promoted,
// even with a /*lang=html*/ marker. This test locks in FR-011 so the N=3
// grammar additions in T044 cannot regress into accepting N>=4.

class NegativeInterpolatedRawN4
{
    // Case 1: N=4 — marker has no effect.
    string a = /*lang=html*/$$$$"""<p>{{{{name}}}}</p>""";
//                                 ^ source.cs - meta.embedded.block.html.cs
//                                  ^ source.cs - entity.name.tag.html

    // Case 2: N=5 — marker has no effect.
    string b = /*lang=html*/$$$$$"""<div>{{{{{user}}}}}</div>""";
//                                  ^ source.cs - meta.embedded.block.html.cs
//                                   ^ source.cs - entity.name.tag.html
}
