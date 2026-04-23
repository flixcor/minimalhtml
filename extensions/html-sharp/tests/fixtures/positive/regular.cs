// US1 positive fixtures: regular C# string literals (double-quoted, non-verbatim,
// non-raw, non-interpolated) preceded by a /*lang=html*/ marker. Each literal
// below MUST be tokenized with html scopes inside the body.

using System;

public class RegularFixtures
{
    // Case 1: canonical single-line marker + literal with escaped attr quotes.
    public string Case1 = /*lang=html*/"<p id=\"x\">hi</p>";

    // Case 2: marker separated from the literal by newlines/whitespace.
    public string Case2 =
        /*lang=html*/
        "<div class=\"a\"><span>two</span></div>";

    // Case 3: escaped quote mid-literal must not terminate the HtmlRegion.
    public string Case3 = /*lang=html*/"<a href=\"/t?q=\\\"x\\\"\">link</a>";
}
