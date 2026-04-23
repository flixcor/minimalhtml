// v3 positive fixtures: non-interpolated string literals passed as the first
// argument to a method named exactly `Html`. Each literal MUST receive HTML
// coloring via the method-trigger grammar rules and parser branch — no
// /*lang=html*/ marker is present.

using System;

public class MethodTriggerRegularFixtures
{
    // Case 1: regular double-quoted string.
    public string Case1() => Html("<p>hello</p>");

    // Case 2: verbatim string.
    public string Case2() => Html(@"<div class=""container""><span>world</span></div>");

    // Case 3: raw 3-quote string.
    public string Case3() => Html("""<section><h1>Title</h1></section>""");

    // Case 4: Html( with spaces before the paren (edge case).
    public string Case4() => Html ("<p>spaced</p>");

    // Case 5: Html( as part of a return statement.
    public string Case5() { return Html("<ul><li>item</li></ul>"); }

    // Case 6: Html( in an assignment.
    public string Case6 = Html("<article><p>text</p></article>");
}

// Placeholder so the parser knows Html exists; not part of the fixtures.
static string Html(string s) => s;
