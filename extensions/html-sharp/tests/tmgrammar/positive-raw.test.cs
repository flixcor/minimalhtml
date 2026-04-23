// SYNTAX TEST "source.cs" "US2 raw-form positive scope assertions"
// Verifies marker-annotated raw 3-quote C# literals ("""...""") emit the
// contract scopes from grammar.md §2.3: comment.block.html-marker.cs on the
// marker, meta.embedded.block.html.cs on the body, entity.name.tag.html on
// tag names, and the C# delimiter scopes preserved on the opening/closing
// """ delimiters (FR-008).

class PositiveRaw
{
    string a = /*lang=html*/"""<p>hi</p>""";
//             ^^^^^^^^^^^^^ comment.block.html-marker.cs
//                          ^^^ punctuation.definition.string.begin.cs
//                             ^ meta.embedded.block.html.cs
//                              ^ entity.name.tag.html
//                                      ^^^ punctuation.definition.string.end.cs

    // Raw with double-quotes mid-body (fewer than three-in-a-row = body).
    string b = /*lang=html*/"""<a href="x" data-q="y">go</a>""";
//             ^^^^^^^^^^^^^ comment.block.html-marker.cs
//                          ^^^ punctuation.definition.string.begin.cs
//                             ^ meta.embedded.block.html.cs
//                              ^ entity.name.tag.html
}
