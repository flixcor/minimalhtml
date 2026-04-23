// SYNTAX TEST "source.cs" "US3 varied marker forms: positive scope assertions"
// Verifies accepted marker variants (block with `language=`, uppercase block,
// line comment) all trigger HTML body coloring per spec US3 acceptance
// scenario 1.

class PositiveVaried
{
    // Block with "language=html" and surrounding whitespace.
    string a = /* language=html */"<p>a</p>";
//             ^^^^^^^^^^^^^^^^^^^ comment.block.html-marker.cs
//                                ^ punctuation.definition.string.begin.cs
//                                 ^ meta.embedded.block.html.cs
//                                  ^ entity.name.tag.html

    // Uppercase block ("LANG=html").
    string b = /*LANG=html*/"<p>b</p>";
//             ^^^^^^^^^^^^^ comment.block.html-marker.cs
//                          ^ punctuation.definition.string.begin.cs
//                           ^ meta.embedded.block.html.cs
//                            ^ entity.name.tag.html

    // Line-comment marker followed by literal on next line.
    string c = // lang=html
        "<p>c</p>";
//      ^ punctuation.definition.string.begin.cs
//       ^ meta.embedded.block.html.cs
//        ^ entity.name.tag.html
}
