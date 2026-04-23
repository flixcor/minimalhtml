// SYNTAX TEST "source.cs" "v3 positive: Html() method trigger on non-interpolated forms"
// Per contracts/grammar.md §2.2 / §3: the Html( token gets
// entity.name.function.html-method-trigger.cs (capture 1) and the string
// body gets meta.embedded.block.html.cs + text.html.basic HTML tag scopes.
// No /*lang=html*/ marker is present — the method name is the trigger.

class MethodTriggerN1
{
    // Case 1: regular double-quoted string.
    // cols: 0         1         2         3
    //       0123456789012345678901234567890
    //                       `Html(` = 15-19, `"` = 20, `<p>` = 21-23, `p` = 22
    string a = Html("<p>x</p>");
//             ^^^^^ entity.name.function.html-method-trigger.cs
//                   ^^^ meta.embedded.block.html.cs
//                    ^ entity.name.tag.html

    // Case 2: verbatim string.
    // cols: 0         1         2         3
    //       01234567890123456789012345678901234
    //                       `Html(` = 15-19, `@"` = 20-21, `<div>` = 22-26, `div` = 23-25
    string b = Html(@"<div>y</div>");
//             ^^^^^ entity.name.function.html-method-trigger.cs
//                    ^^^^^ meta.embedded.block.html.cs
//                     ^^^ entity.name.tag.html

    // Case 3: raw 3-quote string.
    // cols: 0         1         2         3
    //       0123456789012345678901234567890123456
    //                       `Html(` = 15-19, `"""` = 20-22, `<h1>` = 23-26, `h1` = 24-25
    string c = Html("""<h1>z</h1>""");
//             ^^^^^ entity.name.function.html-method-trigger.cs
//                     ^^^^ meta.embedded.block.html.cs
//                      ^^ entity.name.tag.html
}
