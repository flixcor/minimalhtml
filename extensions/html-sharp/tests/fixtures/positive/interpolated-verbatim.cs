// US2 positive fixtures (v2): verbatim interpolated C# string literals
// ($@"..." and @$"...") preceded by a /*lang=html*/ marker. Each literal's
// TEXT SPANS (outside every hole) MUST be tokenized with html scopes; each
// HOLE body MUST retain C# scopes. At N=1, `""` is a literal-quote escape
// (doubled `"` stays inside the string); only a single `"` terminates.

using System;

public class InterpolatedVerbatimFixtures
{
    // Case 1 (US2 Scenario 1): $@"..." single-line, one hole.
    public string Case1(string name) =>
        /*lang=html*/$@"<p>{name}</p>";

    // Case 2 (US2 Scenario 2): @$"..." — order-swapped prefix, one hole.
    public string Case2(string url) =>
        /*lang=html*/@$"<a href=""{url}"">click</a>";

    // Case 3 (US2 Scenario 3): multi-line $@"..." with `""` escape and
    // a hole whose body would be flat text in the v1 skip-branch; here the
    // body-scanner must honor verbatim's newline-allowed and `""`-escape
    // rules and must emit HTML on every text span outside the hole.
    public string Case3(string user, int count) =>
        /*lang=html*/$@"<div class=""u"">
    <span>{user}</span>
    <b>{count}</b>
</div>";
}
