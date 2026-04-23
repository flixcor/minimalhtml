// US1 positive fixtures (v2): regular interpolated C# string literals ($"...")
// preceded by a /*lang=html*/ marker. Each literal's TEXT SPANS (outside every
// hole) MUST be tokenized with html scopes; each HOLE body MUST retain C#
// scopes (no HTML tokenization inside {...}).

using System;

public class InterpolatedRegularFixtures
{
    // Case 1 (spec.md US1 Scenario 1): single hole.
    public string Case1(string name) =>
        /*lang=html*/$"<p>{name}</p>";

    // Case 2 (spec.md US1 Scenario 2): multiple holes, attribute and body.
    public string Case2(string url, string label) =>
        /*lang=html*/$"<a href=\"{url}\">{label}</a>";

    // Case 3 (spec.md Edge Case: nested string literal inside hole).
    // The inner "yes"/"no" literal's quotes MUST NOT terminate the outer literal,
    // and the inner literal's content MUST NOT be HTML-colored.
    public string Case3(bool isOn) =>
        /*lang=html*/$"<p>{(isOn ? "yes" : "no")}</p>";

    // Case 4 (spec.md US1 Scenario 1 variant): escaped quote adjacent to a hole.
    public string Case4(string user) =>
        /*lang=html*/$"<div class=\"u\">{user}</div>";
}
