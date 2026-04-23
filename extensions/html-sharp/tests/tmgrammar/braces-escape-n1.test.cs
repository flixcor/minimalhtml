// SYNTAX TEST "source.cs" "v2 brace escape: N=1 doubled braces are text, format stays in hole"
// At N=1 (regular interpolated), {{ and }} are literal-text brace escapes —
// they do NOT open/close an interpolation hole. Format specifiers (:F2) and
// alignment clauses (,10) stay inside the hole as C# (meta.interpolation.cs).

class BracesEscapeN1
{
    // {{ ok }} — {{ and }} are literal text; first { must NOT be a hole opener.
    string a = /*lang=html*/$"<p>{{ ok }}</p>";
//                            ^^^ meta.embedded.block.html.cs
//                             ^ entity.name.tag.html
//                               ^ -meta.interpolation.cs
//                               ^^ meta.embedded.block.html.cs

    // {x:F2} — the `:F2` format specifier stays inside the hole.
    string b = /*lang=html*/$"<p>{x:F2}</p>";
//                            ^^^ meta.embedded.block.html.cs
//                             ^ entity.name.tag.html
//                                 ^^^ meta.interpolation.cs
}
