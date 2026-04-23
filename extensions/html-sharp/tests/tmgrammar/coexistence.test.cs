// SYNTAX TEST "source.cs" "T060 FR-011: C# non-string tokens retain their scopes adjacent to HtmlRegions"
// The injection MUST NOT leak HTML scopes onto surrounding C# keywords,
// identifiers, or operators. This is the coexistence invariant: a document
// with an HtmlRegion must still render every other token through the host
// C# grammar unchanged.

class Coexistence
{
    // `string` keyword, identifier `a`, operator `=` all adjacent to a
    // marker+literal MUST keep their C# scopes.
    string a = /*lang=html*/"<p>x</p>";
//  ^^^^^^ keyword.type.string.cs
//           ^ keyword.operator.assignment.cs
//                          ^ punctuation.definition.string.begin.cs
//                           ^ meta.embedded.block.html.cs

    // `var` + identifier at the boundary of an HtmlRegion.
    void M()
    {
        var s = /*lang=html*/"<p>y</p>";
//      ^^^ storage.type.var.cs
//                           ^ meta.embedded.block.html.cs
//                                     ^ punctuation.terminator.statement.cs
    }
}
