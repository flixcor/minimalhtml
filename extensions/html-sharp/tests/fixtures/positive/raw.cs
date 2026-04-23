// US2 positive fixtures: raw 3-quote C# string literals ("""...""") preceded
// by a /*lang=html*/ marker. Each literal below MUST be tokenized with html
// scopes inside the body. Only exactly three-in-a-row quotes terminate; fewer
// are body content.

using System;

public class RawFixtures
{
    // Case 1: single-line raw 3-quote.
    public string Case1 = /*lang=html*/"""<input type="text" id="x" autocomplete="">hi</input>""";

    // Case 2: multi-line raw spanning >= 5 lines.
    public string Case2 = /*lang=html*/"""
<section class="wrap">
    <header><h1>title</h1></header>
    <article>
        <p>paragraph</p>
    </article>
</section>
""";

    // Case 3: raw containing double-quotes mid-body (allowed because only
    // three-in-a-row terminate).
    public string Case3 = /*lang=html*/"""<a href="x" data-q=""double"">link</a>""";
}
