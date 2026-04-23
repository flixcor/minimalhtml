// US2 negative fixture (v2): FR-007 — a /*lang=html*/ marker appearing INSIDE
// a hole of an outer marker-annotated interpolated literal does NOT promote
// the inner literal. Only the OUTER literal's text spans (outside the hole)
// receive HTML coloring. The parser MUST emit exactly one HtmlRegion (the
// outer) with the full hole range; no inner region may be emitted.

using System;

public class MarkerInHoleFixtures
{
    // Case 1: inner `/*lang=html*/"inner"` sits inside the outer hole. The
    // outer literal is positive (text spans colored), but the inner string
    // MUST stay flat C#.
    public string Case1(string suffix) =>
        /*lang=html*/$"<p>{(/*lang=html*/"inner" + suffix)}</p>";

    // Case 2: inner $@"..." verbatim inside an outer $"..." hole.
    public string Case2(string x) =>
        /*lang=html*/$"<div>{(/*lang=html*/$@"<span>{x}</span>")}</div>";
}
