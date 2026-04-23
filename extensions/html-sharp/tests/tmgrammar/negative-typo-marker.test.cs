// SYNTAX TEST "source.cs" "US3 typo markers: negative scope assertions"
// Verifies that typo variants (`lang=htm`, `language:html`, `langhtml` with
// no `=`) do NOT trigger HTML body coloring per spec US3 acceptance
// scenario 2.

class NegativeTypo
{
    // "htm" instead of "html" — the \b on html rejects the truncated form.
    string a = /*lang=htm*/"<p>a</p>";
//                          ^ source.cs - meta.embedded.block.html.cs
//                           ^ source.cs - entity.name.tag.html

    // ":" instead of "=" — the \s*=\s* fence rejects the colon.
    string b = /*language:html*/"<p>b</p>";
//                               ^ source.cs - meta.embedded.block.html.cs

    // "langhtml" — no "=" separator — the =\s*html fence rejects it.
    string c = // langhtml
        "<p>c</p>";
//       ^ source.cs - meta.embedded.block.html.cs
}
