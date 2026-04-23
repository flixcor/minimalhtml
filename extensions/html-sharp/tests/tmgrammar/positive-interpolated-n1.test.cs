// SYNTAX TEST "source.cs" "v2 positive: regular interpolated (N=1) text spans + hole bodies"
// Mirrors tests/fixtures/positive/interpolated-regular.cs Cases 1 and 4.
// Per contracts/grammar.md §2.3 / §3: text spans carry meta.embedded.block.html.cs
// and HTML tag scopes; hole bodies whose opener sits outside an HTML tag
// context carry meta.interpolation.cs (the scope the semantic-tokens provider
// uses to suppress HTML token emission inside holes per FR-003, FR-007).
//
// Hole opens inside HTML tag attribute context (e.g. `<a href="{url}">`) are
// NOT asserted here — TextMate's nested pattern resolution means the HTML
// tag-scanner already owns `{` inside `<a ...>`. The parser (T013/T014) is
// the source of truth for hole ranges; the provider (T017) is the source of
// truth for token emission. The grammar's job is to set up the outer block
// scope and color text spans — those are what this test covers.

class InterpolatedRegular
{
    // Case 1: single hole `{name}` — opens in element-body context.
    string a = /*lang=html*/$"<p>{name}</p>";
//                            ^^^ meta.embedded.block.html.cs
//                             ^ entity.name.tag.html
//                                ^^^^ meta.interpolation.cs
//                                     ^^^^ meta.embedded.block.html.cs

    // Case 4: single hole `{user}` — opens after `>` in element-body context.
    string d = /*lang=html*/$"<div class=\"u\">{user}</div>";
//                            ^^^^ meta.embedded.block.html.cs
//                             ^^^ entity.name.tag.html
//                                              ^^^^ meta.interpolation.cs

    // Case 5 (US2): verbatim $@"..." with one hole in element-body context.
    string e = /*lang=html*/$@"<p>{name}</p>";
//                             ^^^ meta.embedded.block.html.cs
//                              ^ entity.name.tag.html
//                                 ^^^^ meta.interpolation.cs

    // Case 6 (US2): verbatim @$"..." — order-swapped prefix.
    string f = /*lang=html*/@$"<p>{name}</p>";
//                             ^^^ meta.embedded.block.html.cs
//                              ^ entity.name.tag.html
//                                 ^^^^ meta.interpolation.cs

    // Case 7 (US2): raw $"""...""" at N=1 with one hole.
    string g = /*lang=html*/$"""<p>{name}</p>""";
//                              ^^^ meta.embedded.block.html.cs
//                               ^ entity.name.tag.html
//                                  ^^^^ meta.interpolation.cs
}
