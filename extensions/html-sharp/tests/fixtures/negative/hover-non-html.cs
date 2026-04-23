// Negative hover fixture: plain C# strings with no HTML marker — no hover should fire.
// Used by tests/unit/hover-provider.test.ts (TH-005).
class HoverNonHtml
{
    string a = "This is just a <string>";
    string b = "Not <html> at all";
}
