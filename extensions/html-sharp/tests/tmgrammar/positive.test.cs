// SYNTAX TEST "source.cs" "US1 regular-form positive scope assertions"
// Column-aligned caret assertions verify that marker-annotated regular
// C# string literals emit the contract scopes from grammar.md §2.3:
// comment.block.html-marker.cs on the marker, meta.embedded.block.html.cs
// on the body, entity.name.tag.html on tag names, and the C# delimiter
// scopes preserved on the opening/closing quotes (FR-008).

class PositiveRegular
{
    string a = /*lang=html*/"<p>hi</p>";
//             ^^^^^^^^^^^^^ comment.block.html-marker.cs
//                          ^ punctuation.definition.string.begin.cs
//                           ^ meta.embedded.block.html.cs
//                            ^ entity.name.tag.html
//                                    ^ punctuation.definition.string.end.cs

    string b = /*lang=html*/"<div><span>two</span></div>";
//             ^^^^^^^^^^^^^ comment.block.html-marker.cs
//                            ^^^ entity.name.tag.html
//                                 ^^^^ entity.name.tag.html

    // Multi-line marker placement: marker terminates a line, opening quote
    // is on the next line. Requires the #marker-regular-multiline grammar
    // pattern (TextMate begin patterns are line-bounded, so the same-line
    // form alone cannot match this layout).
    string c =
        /*lang=html*/
        "<p>multi</p>";
//      ^ punctuation.definition.string.begin.cs
//       ^ meta.embedded.block.html.cs
//        ^ entity.name.tag.html
//                   ^ punctuation.definition.string.end.cs
}
