// v3 coexistence fixture: mixes /*lang=html*/ marker-triggered strings with
// Html() method-triggered strings in the same file. Both MUST receive HTML
// coloring independently without conflict.

using System;

public class CoexistenceFixtures
{
    // Case 1: marker-triggered interpolated string.
    public string Case1(string x) => /*lang=html*/$"<p>{x}</p>";

    // Case 2: method-triggered plain string.
    public string Case2() => Thing.Html("<span>marker-free</span>");

    // Case 3: marker-triggered on one line, method-triggered on the next.
    public string Case3a = /*lang=html*/"<div>marker</div>";
    public string Case3b() => Html("<div>method</div>");

    // Case 4: method trigger with marker comment INSIDE the argument (dual-trigger
    // scenario from research.md R5). The method rule fires; the inner comment is
    // painted as HTML text, not as a second trigger.
    public string Case4() => Html(/*lang=html*/"<p>dual</p>");
}

public static class Thing
{
    public static string Html(string s) => s;
}

static string Html(string s) => s;
