// SYNTAX TEST "source.cs" "v3 negative: Html identifier without ( does not trigger"
// A bare `Html` field name adjacent to a string literal must NOT produce
// meta.embedded.block.html.cs. The method trigger requires `Html(`.

class NegativeNoParen
{
    // Case 1: `Html` as a string field (no method call).
    // Code: `    string Html = "<p>...`
    //        0         1
    //        0123456789012345678901...
    //                         ^ col 19 = `<`
    string Html = "<p>no color</p>";
//                 ^^^ -meta.embedded.block.html.cs

    // Case 2: `Html` field, string literal on next line (no paren).
    // Code (second line): `        "<p>...`
    //                      0123456789...
    //                              ^^ col 8=`"`, col 9=`<`
    string Html2 =
        "<p>no color</p>";
//       ^^^ -meta.embedded.block.html.cs
}
