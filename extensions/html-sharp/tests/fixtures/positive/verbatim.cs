// US2 positive fixtures: verbatim C# string literals (@"...") preceded by a
// /*lang=html*/ marker. Each literal below MUST be tokenized with html scopes
// inside the body. In verbatim form, "" is the escape for a literal quote;
// backslashes are NOT escapes.

using System;

public class VerbatimFixtures
{
    // Case 1: single-line verbatim with marker.
    public string Case1 = /*lang=html*/@"<p id=""x"">hi</p>";

    // Case 2: multi-line verbatim with marker containing "" escape.
    public string Case2 = /*lang=html*/@"<div class=""a"">
    <span>multi
    line</span>
</div>";

    // Case 3: verbatim with attribute quote escaped as "".
    public string Case3 = /*lang=html*/@"<a href=""/t?q=x"">link</a>";
}
