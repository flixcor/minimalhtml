// US3 positive fixtures: varied accepted marker forms per spec US3
// acceptance scenario 1. Every marker below MUST be recognized and the
// associated regular C# string literal MUST be tokenized with HTML scopes.

using System;

public class MarkersVariedFixtures
{
    // Case 1: line-comment marker with lang=html at end of assignment line.
    public string Case1 = // lang=html
        "<p>a</p>";

    // Case 2: line-comment marker with language=html.
    public string Case2 = // language=html
        "<p>b</p>";

    // Case 3: uppercase line-comment marker.
    public string Case3 = //LANG=HTML
        "<p>c</p>";

    // Case 4: canonical block-comment marker (already covered by US1 but
    // included here so the US3 test asserts all seven cases uniformly).
    public string Case4 = /*lang=html*/"<p>d</p>";

    // Case 5: block with language=html and surrounding whitespace.
    public string Case5 = /* language=html */"<p>e</p>";

    // Case 6: uppercase block-comment marker.
    public string Case6 = /*LANG=html*/"<p>f</p>";

    // Case 7: block with mixed case and generous internal whitespace.
    public string Case7 = /* Lang = Html */"<p>g</p>";
}
