// Polish negative fixture: marker is present, but the FIRST token after it
// is not a C# string-literal opener. These MUST NOT promote the next-seen
// literal later in the file. parseHtmlRegions() MUST return [] for this
// file.

using System;

public class MarkerBeforeNonStringFixtures
{
    // Case 1: marker before integer literal.
    public int    Case1a = /*lang=html*/42;
    public string Case1b = "<p>a</p>";

    // Case 2: marker before bool literal.
    public bool   Case2a = /*lang=html*/true;
    public string Case2b = "<p>b</p>";

    // Case 3: marker before null.
    public object Case3a = /*lang=html*/null;
    public string Case3b = "<p>c</p>";

    // Case 4: marker before identifier.
    public string Case4a = /*lang=html*/String.Empty;
    public string Case4b = "<p>d</p>";

    // Case 5: marker before char literal.
    public char   Case5a = /*lang=html*/'x';
    public string Case5b = "<p>e</p>";

    // Case 6: marker before floating-point literal.
    public double Case6a = /*lang=html*/3.14;
    public string Case6b = "<p>f</p>";

    // Case 7: marker before array expression.
    public int[]  Case7a = /*lang=html*/new[] { 1, 2, 3 };
    public string Case7b = "<p>g</p>";

    // Case 8: marker before open brace (object initializer).
    public object Case8a = /*lang=html*/new { X = 1 };
    public string Case8b = "<p>h</p>";

    // Case 9: marker before keyword expression (default).
    public int    Case9a = /*lang=html*/default;
    public string Case9b = "<p>i</p>";

    // Case 10: marker before parenthesized tuple.
    public (int, int) Case10a = /*lang=html*/(1, 2);
    public string     Case10b = "<p>j</p>";
}
