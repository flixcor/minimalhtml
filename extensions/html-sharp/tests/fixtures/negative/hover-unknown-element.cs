// Negative hover fixture: unknown/custom element — should return undefined gracefully.
// Used by tests/unit/hover-provider.test.ts (TH-008).
class HoverUnknownElement
{
    string a = /*lang=html*/ "<my-widget>content</my-widget>";
}
