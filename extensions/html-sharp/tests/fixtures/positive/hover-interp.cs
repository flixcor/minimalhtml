// Hover fixtures: interpolated strings — text spans get hover, holes do not.
// Used by tests/unit/hover-provider.test.ts (TH-003, TH-004).
class HoverInterp
{
    string a = /*lang=html*/ $"<p>{name}</p>";
    string b = Html($"<a href=\"{url}\">link</a>");
}
