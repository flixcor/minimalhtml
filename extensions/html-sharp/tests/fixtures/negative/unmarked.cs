// US1 negative fixtures: regular literals that MUST remain flat C# string
// coloring. parseHtmlRegions() MUST return [] for this file.

using System;

public class UnmarkedFixtures
{
    // Case 1: no marker at all.
    public string Plain = "<p>not html</p>";

    // Case 2: the text "lang=html" appears inside the literal but not as a
    // preceding comment — FR-007 requires the marker to be a block comment
    // immediately before the opening delimiter, not text within the string.
    public string Textual = "config: lang=html";

    // Case 3: line-comment marker IS a valid US3 marker form, but the NEXT
    // non-whitespace token is the `public` keyword — not a string-literal
    // opener. Adjacency (FR-007) therefore fails and no promotion occurs.
    // lang=html
    public string LineCommented = "<p>still flat</p>";

    // Case 3b: block-comment marker followed by a keyword before the literal
    // (same adjacency violation as 3, via block form).
    /*lang=html*/
    public string KeywordBetween = "<p>flat too</p>";

    // Case 4: marker separated from a literal by a non-whitespace, non-string
    // token (here: the identifier `Plain`). The first literal after the
    // marker is not immediately preceded by it, so the marker does not apply.
    public string Interposed = /*lang=html*/ Plain + "<p>late</p>";

    // Case 5: marker followed by a non-string expression terminates the
    // association before any literal is reached.
    public int Numeric = /*lang=html*/ 42;
    public string AfterNumeric = "<p>detached</p>";

    // Case 6: marker followed by `+` unary, then literal — unary operator
    // breaks adjacency even though there's no identifier between them.
    public string UnaryPlus = /*lang=html*/ + "<p>k</p>";

    // Case 7: marker immediately followed by a non-string declaration — no
    // literal within adjacency reach.
    public int AfterIsGone = /*lang=html*/0;

    // Case 8: block comment that is NOT a marker (missing `=`) — should be
    // ignored entirely.
    public string NotAMarker = /* lang html */"<p>m</p>";
}
