// US3 positive fixtures (v2): brace escapes ({{/}} at N=1, shorter-than-N runs
// at N>=2) render as HTML text; format specifiers (:F2) and alignment clauses
// (,10) stay inside the interpolation hole as C#.

using System;

public class BracesModifiersFixtures
{
    // Case 1 (N=1, brace escape): {{ and }} are literal text — zero holes.
    public string Case1() =>
        /*lang=html*/$"<p style=\"{{ color: red }}\">text</p>";

    // Case 2 (N=1, format specifier): {price:F2} — :F2 is inside the hole.
    public string Case2(double price) =>
        /*lang=html*/$"<p>{price:F2}</p>";

    // Case 3 (N=1, alignment): {label,10} — ,10 is inside the hole.
    public string Case3(string label) =>
        /*lang=html*/$"<p>{label,10}</p>";

    // Case 4 (N=1, combined format+alignment): {value,-5:X} — ,-5:X inside hole.
    public string Case4(int value) =>
        /*lang=html*/$"<p>{value,-5:X}</p>";

    // Case 5 (N=2, lone brace): single { and } are literal text; {{name}} is the hole.
    public string Case5(string name) =>
        /*lang=html*/$$"""<p style="{ color: blue }">{{name}}</p>""";

    // Case 6 (N=3, run-of-1 brace as text): single { is literal; {{{name}}} is hole.
    public string Case6(string name) =>
        /*lang=html*/$$$"""<p>{ {{{name}}} }</p>""";

    // Case 7 (N=3, run-of-2 brace as text): {{ is literal text; {{{name}}} is hole.
    public string Case7(string name) =>
        /*lang=html*/$$$"""<p>{{ {{{name}}} }}</p>""";
}
