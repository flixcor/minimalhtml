// US2 positive fixtures (v2): raw interpolated at N=3 — exactly three leading
// `$$$` and a `"""` delimiter. At N=3, the hole opener is EXACTLY `{{{` and
// the hole closer is EXACTLY `}}}`. Runs of 1 or 2 `{`/`}` in a text span
// are literal HTML text per FR-004.

using System;

public class InterpolatedRawN3Fixtures
{
    // Case 1: $$$"""...""" single-line with one `{{{...}}}` hole.
    public string Case1(string name) =>
        /*lang=html*/$$$"""<p>{{{name}}}</p>""";

    // Case 2: multi-line with a `{{{...}}}` hole whose body contains
    // format-like text. Runs of 1 or 2 `{`/`}` are literal HTML.
    public string Case2(double value) =>
        /*lang=html*/$$$"""
<div>
    <span>literal {{ and }} stay as text</span>
    <b>value: {{{value:F2}}}</b>
</div>
""";
}
