// US2 negative fixture (v2): raw interpolated at N>=4 stays INERT even when
// a /*lang=html*/ marker precedes it (FR-011). The parser MUST return [] on
// this fixture; the grammar MUST NOT apply meta.embedded.block.html.cs.

using System;

public class InterpolatedRawN4Fixtures
{
    // Case 1: N=4 leading `$` with marker — still inert.
    public string Case1(string name) =>
        /*lang=html*/$$$$"""<p>{{{{name}}}}</p>""";

    // Case 2: N=5 leading `$` with marker — still inert.
    public string Case2(string user) =>
        /*lang=html*/$$$$$"""<div>{{{{{user}}}}}</div>""";
}
