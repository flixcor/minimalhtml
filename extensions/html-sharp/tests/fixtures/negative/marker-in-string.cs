// Polish negative fixture: marker-like text that appears INSIDE another
// string or inside a line-comment's preceding-string context MUST NOT
// promote a subsequent literal. parseHtmlRegions() MUST return [] for
// this file.

using System;

public class MarkerInStringFixtures
{
    // Case 1: marker text inside a preceding string literal (not a comment).
    public string Case1a = "/*lang=html*/";
    public string Case1b = "<p>a</p>";

    // Case 2: marker text inside a verbatim string.
    public string Case2a = @"/*lang=html*/";
    public string Case2b = "<p>b</p>";

    // Case 3: marker text inside a raw string.
    public string Case3a = """/*lang=html*/""";
    public string Case3b = "<p>c</p>";

    // Case 4: marker text appears as the VALUE of a prior literal, with the
    // next literal on the next line — still not a comment, so inert.
    public string Case4a = "lang=html is a setting key";
    public string Case4b = "<p>d</p>";

    // Case 5: marker-looking text in a char literal.
    public char   Case5a = '/';
    public string Case5b = "/*lang=html*/"
                         + "<p>e</p>";

    // Case 6: marker-looking text in an interpolated hole of an inert literal.
    public string Case6a = $"{/*lang=html*/null}";
    public string Case6b = "<p>f</p>";

    // Case 7: block-comment lookalike inside a string (with escaped quotes).
    public string Case7a = "prefix /*lang=html*/ suffix";
    public string Case7b = "<p>g</p>";

    // Case 8: line-comment lookalike inside a verbatim string body.
    public string Case8a = @"// lang=html is not a real marker here";
    public string Case8b = "<p>h</p>";

    // Case 9: marker appears only inside a raw string body that contains a
    // newline, so regex must not match across the string.
    public string Case9a = """
        // lang=html (this is body text, not a comment)
        """;
    public string Case9b = "<p>i</p>";

    // Case 10: marker-style token concatenated from string parts.
    public string Case10a = "/*lang" + "=html*/";
    public string Case10b = "<p>j</p>";
}
