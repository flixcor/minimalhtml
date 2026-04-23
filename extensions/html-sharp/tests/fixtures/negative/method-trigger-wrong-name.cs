// v3 negative fixtures: strings passed to methods with names that are NOT
// exactly `Html`. None of these MUST receive HTML coloring.

using System;

public class MethodTriggerWrongNameFixtures
{
    // Case 1: lowercase `html` — case-sensitive mismatch.
    public string Case1() => html("<p>no color</p>");

    // Case 2: all-caps `HTML` — case-sensitive mismatch.
    public string Case2() => HTML("<p>no color</p>");

    // Case 3: `HtmlHelper` — word boundary guard: `Html` is a prefix, not the whole identifier.
    public string Case3() => HtmlHelper("<p>no color</p>");

    // Case 4: `myHtml` — word boundary guard: `Html` is a suffix.
    public string Case4() => myHtml("<p>no color</p>");

    // Case 5: `htmlContent` — different name.
    public string Case5() => htmlContent("<p>no color</p>");

    // Case 6: `OtherMethod` — completely different name.
    public string Case6() => OtherMethod("<p>no color</p>");

    // Case 7: bare `Html` identifier (no opening paren) — must not trigger.
    public string Html = "<p>no color</p>";
}

static string html(string s) => s;
static string HTML(string s) => s;
static string HtmlHelper(string s) => s;
static string myHtml(string s) => s;
static string htmlContent(string s) => s;
static string OtherMethod(string s) => s;
