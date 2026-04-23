// Hover fixtures: attribute hover inside HTML-colored strings.
// Used by tests/unit/hover-provider.test.ts (TH-006, TH-010, TH-011).
class HoverAttribute
{
    string a = Html("<button disabled type=\"submit\">OK</button>");
    string b = Html("<a href=\"#\" target=\"_blank\">link</a>");
}
