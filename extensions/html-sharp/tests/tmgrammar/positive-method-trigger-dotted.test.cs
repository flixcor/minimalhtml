// SYNTAX TEST "source.cs" "v6 positive: dotted receiver Html() trigger"
// When Html() is called as a dotted method (receiver.Html("...")), the
// updated injection grammar begin pattern
//   ((?:[\w.>()]+\.)?(?<!\w)Html\s*\()
// matches the entire receiver.Html( span as capture 1, assigning it
// entity.name.function.html-method-trigger.cs. The string body receives
// meta.embedded.block.html.cs + HTML tag scopes.

class DottedMethodTrigger
{
    // Case 1: simple identifier receiver.
    // obj.Html( spans positions 15-23 (9 chars), " at 24, <p> at 25-27, p at 26.
    string a = obj.Html("<p>x</p>");
//             ^^^^^^^^^ entity.name.function.html-method-trigger.cs
//                       ^^^ meta.embedded.block.html.cs
//                        ^ entity.name.tag.html

    // Case 2: static class name receiver.
    // MyClass.Html( spans positions 15-27 (13 chars), " at 28, <h1> at 29-32, h1 at 30-31.
    string b = MyClass.Html("<h1>y</h1>");
//             ^^^^^^^^^^^^^ entity.name.function.html-method-trigger.cs
//                           ^^^^ meta.embedded.block.html.cs
//                            ^^ entity.name.tag.html
}
