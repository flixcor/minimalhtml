// SYNTAX TEST "source.cs" "v2 brace escape at N=3"
// At N=3 (raw interpolated with $$$"""..."""), runs of ONE or TWO { or } are
// literal text — NOT hole openers/closers. Only a run of EXACTLY THREE {{{
// or }}} opens or closes an interpolation hole.

class BracesEscapeN3
{
    // Case a: single { and } are literal text; {{{x}}} is the hole.
    string a = /*lang=html*/$$$"""<p>{ {{{x}}} }</p>""";
//                                ^^^ meta.embedded.block.html.cs
//                                 ^ entity.name.tag.html
//                                   ^ -meta.interpolation.cs
//                                   ^ meta.embedded.block.html.cs
//                                       ^ meta.interpolation.cs
//                                             ^ -meta.interpolation.cs
//                                             ^ meta.embedded.block.html.cs

    // Case b: {{ and }} (run of two) are also literal text at N=3.
    string b = /*lang=html*/$$$"""<p>{{ {{{x}}} }}</p>""";
//                                ^^^ meta.embedded.block.html.cs
//                                 ^ entity.name.tag.html
//                                   ^^ -meta.interpolation.cs
//                                   ^^ meta.embedded.block.html.cs
//                                        ^ meta.interpolation.cs
//                                              ^^ -meta.interpolation.cs
//                                              ^^ meta.embedded.block.html.cs
}
