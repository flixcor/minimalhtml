// SYNTAX TEST "source.cs" "US1 negative: unmarked and 4-quote-raw literals stay inert"
// Asserts that meta.embedded.block.html.cs and entity.name.tag.html are
// ABSENT inside the bodies of literals that lack the marker or violate the
// N≤3 raw-quote rule (FR-006, SC-002, SC-003a). Marker-annotated
// interpolated cases were REMOVED in v2 T018 — they are positive under v2
// FR-006 and covered by tests/tmgrammar/positive-interpolated-n1.test.cs.

class NegativeUnmarked
{
    // No marker at all: body must stay flat C# string coloring.
    string plain = "<p>not html</p>";
//                   ^ source.cs - meta.embedded.block.html.cs
//                    ^ source.cs - entity.name.tag.html

    // "lang=html" as literal text, not as a block comment: still inert.
    string textual = "config: lang=html";
//                     ^ source.cs - meta.embedded.block.html.cs

    // 4-quote raw literal with marker: unsupported in v1 per research.md R3
    // (raws with N>3 opening quotes), MUST stay flat even with the marker.
    string rawFour = /*lang=html*/""""<p>nope</p>"""";
//                                    ^ source.cs - meta.embedded.block.html.cs
//                                     ^ source.cs - entity.name.tag.html

    // Polish T058: marker followed by identifier (adjacency violated).
    string notAdjacent = /*lang=html*/ Plain + "<p>late</p>";
//                                                ^ source.cs - meta.embedded.block.html.cs

    // Polish T058: marker followed by integer (non-string).
    int    beforeNum    = /*lang=html*/42;
    string afterBeforeNum = "<p>after</p>";
//                             ^ source.cs - meta.embedded.block.html.cs

    string Plain = "";

    // US2 T038 / FR-007: a /*lang=html*/ marker INSIDE a hole of an outer
    // marker-annotated literal does NOT promote the inner literal. Only the
    // outer's text spans (outside the hole) are HTML-colored; the inner
    // literal's body stays flat C#.
    string markerInHole = /*lang=html*/$"<p>{(/*lang=html*/"<span>inner</span>" + suffix)}</p>";
//                                                           ^ source.cs - entity.name.tag.html

    // T048 regression: when an inner /*lang=html*/-annotated verbatim literal
    // sits inside the outer hole, the outer marker's begin regex must NOT
    // non-greedy-skip across the first `*/` and treat the two markers as one.
    // The text BEFORE the outer hole (`<div>`) stays HTML; the text INSIDE
    // the inner literal body (`<span>inner</span>`) stays flat C#.
    string markerInHoleVerbatim = /*lang=html*/$"<div>{(/*lang=html*/$@"<span>inner</span>")}</div>";
//                                                ^ entity.name.tag.html
//                                                                     ^ source.cs - entity.name.tag.html
//                                                                                             ^ entity.name.tag.html
}
