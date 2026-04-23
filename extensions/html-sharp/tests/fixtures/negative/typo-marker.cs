// US3 negative fixtures: typo marker variants per spec US3 acceptance
// scenario 2. Every marker below is a TYPO and MUST NOT be recognized —
// parseHtmlRegions() MUST return [] for this file.

using System;

public class TypoMarkerFixtures
{
    // Case 1: htm instead of html.
    public string Case1 = /*lang=htm*/"<p>a</p>";

    // Case 2: : instead of =.
    public string Case2 = /*language:html*/"<p>b</p>";

    // Case 3: languag (missing terminal "e" of "language" and no "lang" token
    // on its own because `=` is required immediately after `lang` or
    // `language`, not after `languag`).
    public string Case3 = /*languag=html*/"<p>c</p>";

    // Case 4: no "=" between keyword and value — line comment form.
    public string Case4 = // langhtml
        "<p>d</p>";

    // Case 5: "html" split by a whitespace in the middle — line comment form.
    public string Case5 = // lang = ht ml
        "<p>e</p>";
}
