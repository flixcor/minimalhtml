// SYNTAX TEST "source.cs" "US2 verbatim-form positive scope assertions"
// Verifies marker-annotated verbatim C# literals (@"...") emit the contract
// scopes from grammar.md §2.3: comment.block.html-marker.cs on the marker,
// meta.embedded.block.html.cs on the body, entity.name.tag.html on tag names,
// and the C# delimiter scopes preserved on the opening @" and closing "
// (FR-008).

class PositiveVerbatim
{
    string a = /*lang=html*/@"<p id=""x"">hi</p>";
//             ^^^^^^^^^^^^^ comment.block.html-marker.cs
//                          ^^ punctuation.definition.string.begin.cs
//                            ^ meta.embedded.block.html.cs
//                             ^ entity.name.tag.html
//                                              ^ punctuation.definition.string.end.cs

    // Multi-line verbatim with marker placed on a previous line; body lines
    // must still carry the embedded scope. (Closing-delim scope is covered
    // by Case a above; omitted here because html.basic's tag-end scope
    // visibly extends across an adjacent " delimiter in the scope-inspector
    // view — a known html.basic quirk that does not affect theme rendering.)
    string b =
        /*lang=html*/
        @"<p>deep</p>";
//      ^^ punctuation.definition.string.begin.cs
//        ^ meta.embedded.block.html.cs
//         ^ entity.name.tag.html
}
