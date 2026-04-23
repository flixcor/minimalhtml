// SYNTAX TEST "source.cs" "v3 negative: wrong method names do not trigger Html coloring"
// Per contracts/grammar.md §3: html(, HTML(, HtmlHelper(, myHtml( produce
// NO meta.embedded.block.html.cs on string bodies (FR-004).

class NegativeWrongName
{
    // Case 1: lowercase `html(` — case mismatch.
    // cols: `    string a = html("` = 21 chars; `<p>` at 21-23
    string a = html("<p>no color</p>");
//                   ^^^ -meta.embedded.block.html.cs

    // Case 2: all-caps `HTML(` — case mismatch.
    // cols: `    string b = HTML("` = 21 chars; `<p>` at 21-23
    string b = HTML("<p>no color</p>");
//                   ^^^ -meta.embedded.block.html.cs

    // Case 3: `HtmlHelper(` — word-boundary: `Html` is a prefix, not the full name.
    // cols: `    string c = HtmlHelper("` = 27 chars; `<p>` at 27-29
    string c = HtmlHelper("<p>no color</p>");
//                         ^^^ -meta.embedded.block.html.cs

    // Case 4: `myHtml(` — word-boundary: `Html` is a suffix.
    // cols: `    string d = myHtml("` = 23 chars; `<p>` at 23-25
    string d = myHtml("<p>no color</p>");
//                     ^^^ -meta.embedded.block.html.cs
}
