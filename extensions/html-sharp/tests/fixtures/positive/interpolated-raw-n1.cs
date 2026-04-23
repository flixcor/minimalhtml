// US2 positive fixtures (v2): raw interpolated at N=1 — exactly one leading
// `$` and a `"""` delimiter. At N=1, hole opener is a single `{`; a literal
// `"` (run of 1 or 2, not 3) stays inside the string — only three quotes
// in a row terminate.

using System;

public class InterpolatedRawN1Fixtures
{
    // Case 1 (US2 Scenario 4a): single-line $"""...""" with one hole.
    public string Case1(string name) =>
        /*lang=html*/$"""<p>{name}</p>""";

    // Case 2 (US2 Scenario 4b): multi-line $"""...""" spanning five lines
    // with multiple holes — body-scanner must honor multi-line and exactly-1
    // brace-run semantics identically to single-line.
    public string Case2(string user, int count, string href) =>
        /*lang=html*/$"""
<div class="u">
    <a href="{href}">{user}</a>
    <b>{count}</b>
</div>
""";

    // Case 3: literal `"` inside the body — only a run of three quotes
    // terminates, a single or double `"` is literal text.
    public string Case3(string label) =>
        /*lang=html*/$"""<p data="x">{label} says "hello"</p>""";
}
