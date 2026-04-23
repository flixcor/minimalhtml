// v3 positive fixtures: interpolated string literals passed as the first
// argument to Html(). Text spans MUST receive HTML coloring; holes MUST
// retain C# coloring. No /*lang=html*/ marker.

using System;

public class MethodTriggerInterpFixtures
{
    // Case 1: regular interpolated $"...".
    public string Case1(string name) => Html($"<p>{name}</p>");

    // Case 2: verbatim interpolated $@"...".
    public string Case2(string cls) => Html($@"<div class=""{cls}""><span>text</span></div>");

    // Case 3: verbatim interpolated @$"..." (swapped prefix).
    public string Case3(string id) => Html(@$"<section id=""{id}"">body</section>");

    // Case 4: raw interpolated $"""...""" (N=1).
    public string Case4(string val) => Html($"""<p class="x">{val}</p>""");

    // Case 5: raw interpolated $$"""...""" (N=2).
    public string Case5(string val) => Html($$"""<p>{{val}}</p>""");

    // Case 6: raw interpolated $$$"""...""" (N=3).
    public string Case6(string val) => Html($$$"""<p>{{{val}}}</p>""");

    // Case 7: N=4 $$$$"""...""" — MUST NOT trigger (inert per FR-007).
    public string Case7(string val) => Html($$$$"""<p>{{{{val}}}}</p>""");
}

static string Html(string s) => s;
