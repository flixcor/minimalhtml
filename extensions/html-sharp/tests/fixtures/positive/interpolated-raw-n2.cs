// US2 positive fixtures (v2): raw interpolated at N=2 — exactly two leading
// `$$` and a `"""` delimiter. At N=2, the hole opener is EXACTLY `{{` and the
// hole closer is EXACTLY `}}`. A single `{` or `}` in a text span is literal
// HTML text (NOT a hole delimiter) per FR-004.

using System;

public class InterpolatedRawN2Fixtures
{
    // Case 1 (US2 Scenario 5): $$"""...""" single-line, one `{{...}}` hole.
    public string Case1(string name) =>
        /*lang=html*/$$"""<p>{{name}}</p>""";

    // Case 2: multi-line with multiple `{{...}}` holes.
    public string Case2(string user, int count) =>
        /*lang=html*/$$"""
<div>
    <span>{{user}}</span>
    <b>{{count}}</b>
</div>
""";

    // Case 3: single `{` or `}` in a text span MUST stay HTML (not a hole).
    // The `{ color: red }` reads as CSS-ish text, not a hole opener.
    public string Case3(string style) =>
        /*lang=html*/$$"""<p style="{{style}}">{ color: red }</p>""";
}
