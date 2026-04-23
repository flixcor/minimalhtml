// SYNTAX TEST "source.cs" "v3 positive: Html() method trigger on interpolated forms"
// Per contracts/grammar.md §3: Html( token gets
// entity.name.function.html-method-trigger.cs; text spans get
// meta.embedded.block.html.cs; hole bodies get meta.interpolation.cs.

class MethodTriggerInterp
{
    // Case 1: regular interpolated $"...".
    // cols: `    string a = Html($"<p>{name}</p>");`
    //  0         1         2         3
    //  01234567890123456789012345678901234
    //                 ^Html( = 15   ^< = 22  ^{ = 25  ^n = 26
    string a = Html($"<p>{name}</p>");
//             ^^^^^ entity.name.function.html-method-trigger.cs
//                    ^^^ meta.embedded.block.html.cs
//                        ^^^^ meta.interpolation.cs
//                             ^^^^ meta.embedded.block.html.cs

    // Case 2: verbatim interpolated $@"...".
    // cols: `    string b = Html($@"<p>{name}</p>");`
    //  0         1         2         3
    //  0123456789012345678901234567890123456
    //                 ^Html( = 15  ^< = 23  ^{ = 26
    string b = Html($@"<p>{name}</p>");
//             ^^^^^ entity.name.function.html-method-trigger.cs
//                     ^^^ meta.embedded.block.html.cs
//                         ^^^^ meta.interpolation.cs

    // Case 3: raw-n1 interpolated $"""...""".
    // cols: `    string c = Html($"""<p>{name}</p>""");`
    //  0         1         2         3
    //  01234567890123456789012345678901234567890
    //                 ^Html( = 15    ^< = 24  ^{ = 27
    string c = Html($"""<p>{name}</p>""");
//             ^^^^^ entity.name.function.html-method-trigger.cs
//                      ^^^ meta.embedded.block.html.cs
//                          ^^^^ meta.interpolation.cs

    // Case 4: raw-n2 interpolated $$"""...{{x}}...""".
    // cols: `    string d = Html($$"""<p>{{name}}</p>""");`
    //  0         1         2         3
    //  0123456789012345678901234567890123456789012
    //                 ^Html( = 15     ^< = 25  ^{ = 28
    string d = Html($$"""<p>{{name}}</p>""");
//             ^^^^^ entity.name.function.html-method-trigger.cs
//                       ^^^ meta.embedded.block.html.cs
//                           ^^^^ meta.interpolation.cs

    // Case 5: raw-n3 interpolated $$$"""...{{{x}}}...""".
    // cols: `    string e = Html($$$"""<p>{{{name}}}</p>""");`
    //  0         1         2         3
    //  01234567890123456789012345678901234567890123456
    //                 ^Html( = 15      ^< = 26  ^{ = 29
    string e = Html($$$"""<p>{{{name}}}</p>""");
//             ^^^^^ entity.name.function.html-method-trigger.cs
//                        ^^^ meta.embedded.block.html.cs
//                            ^^^^ meta.interpolation.cs
}
