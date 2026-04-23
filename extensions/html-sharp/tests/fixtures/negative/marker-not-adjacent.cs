// Polish negative fixture: marker present, but the string literal is NOT
// immediately adjacent (only whitespace is allowed between marker and
// opening delimiter per FR-007). Every literal below MUST stay flat —
// parseHtmlRegions() MUST return [] for this file.

using System;

public class MarkerNotAdjacentFixtures
{
    // Case 1: marker, identifier, then literal — identifier breaks adjacency.
    public string Case1 = /*lang=html*/ String.Empty + "<p>a</p>";

    // Case 2: marker, parenthesized expression, then literal.
    public string Case2 = /*lang=html*/(true ? "<p>b</p>" : "");

    // Case 3: marker, method call, then literal on a continued line.
    public string Case3 = /*lang=html*/ GetPrefix() +
        "<p>c</p>";

    // Case 4: marker separated from literal by a semicolon (statement break).
    public string Case4;
    public void Init4() { Case4 = /*lang=html*/null; Case4 = "<p>d</p>"; }

    // Case 5: marker, cast, then literal.
    public string Case5 = /*lang=html*/(string)"<p>e</p>";

    // Case 6: marker at end of one assignment, literal in the NEXT assignment.
    public object Case6a = /*lang=html*/42;
    public string Case6b = "<p>f</p>";

    // Case 7: marker, new-expression, then literal as argument.
    public string Case7 = /*lang=html*/new string("<p>g</p>".ToCharArray());

    // Case 8: marker, chained member access, then literal.
    public string Case8 = /*lang=html*/Case1.Substring(0) + "<p>h</p>";

    // Case 9: marker, negation/unary, then literal.
    public string Case9 = /*lang=html*/ !false ? "<p>i</p>" : "";

    // Case 10: marker, numeric literal, plus-concat, then string.
    public string Case10 = /*lang=html*/1 + "<p>j</p>";

    // Helpers (not counted as negative cases — unmarked literals).
    private static string GetPrefix() => "prefix:";
}
