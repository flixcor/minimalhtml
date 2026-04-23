// SYNTAX TEST "source.cs" "v2 brace escape at N=2"
// At N=2 (raw interpolated with $$"""..."""), a run of EXACTLY ONE { or } is
// literal text — NOT a hole opener/closer. Only a run of exactly TWO {{ or }}
// opens or closes an interpolation hole.

class BracesEscapeN2
{
    // Lone { and } in the body are literal text; {{x}} is the hole.
    string a = /*lang=html*/$$"""<p>{ {{x}} }</p>""";
//                               ^^^ meta.embedded.block.html.cs
//                                ^ entity.name.tag.html
//                                  ^ -meta.interpolation.cs
//                                  ^ meta.embedded.block.html.cs
//                                      ^ meta.interpolation.cs
//                                            ^ -meta.interpolation.cs
//                                            ^ meta.embedded.block.html.cs
}
