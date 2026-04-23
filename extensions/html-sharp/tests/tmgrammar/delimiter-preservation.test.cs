// SYNTAX TEST "source.cs" "T059 FR-008: delimiter scopes preserved on every form"
// For each supported HtmlRegion form (regular, verbatim, raw3), the opening
// delimiter MUST retain `punctuation.definition.string.begin.cs` and the
// closing delimiter MUST retain `punctuation.definition.string.end.cs`.
// This is the stability invariant documented in grammar.md §2.3 and is the
// single-source-of-truth test for FR-008.

class DelimiterPreservation
{
    // Regular form, same line.
    string a = /*lang=html*/"<p>a</p>";
//                          ^ punctuation.definition.string.begin.cs
//                                   ^ punctuation.definition.string.end.cs

    // Verbatim form, same line.
    string b = /*lang=html*/@"<p>b</p>";
//                          ^^ punctuation.definition.string.begin.cs
//                                    ^ punctuation.definition.string.end.cs

    // Raw-3 form, same line.
    string c = /*lang=html*/"""<p>c</p>""";
//                          ^^^ punctuation.definition.string.begin.cs
//                                     ^^^ punctuation.definition.string.end.cs
}
