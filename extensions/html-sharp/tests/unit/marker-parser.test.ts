import * as assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import * as path from "node:path";
import { parseHtmlRegions, textSpansOf } from "../../src/marker-parser";

const REPO_ROOT = path.resolve(__dirname, "..", "..", "..");
const FIXTURE = (...p: string[]) =>
  readFileSync(path.resolve(REPO_ROOT, "tests", "fixtures", ...p), "utf8");

describe("marker-parser", () => {
  it("returns no regions for empty input", () => {
    assert.deepEqual(parseHtmlRegions(""), []);
  });

  it("returns no regions for plain C# code with no markers", () => {
    const text = 'var s = "hello";\n';
    assert.deepEqual(parseHtmlRegions(text), []);
  });

  describe("US1 — regular literals with /*lang=html*/ marker", () => {
    it("detects all three cases in positive/regular.cs", () => {
      const text = FIXTURE("positive", "regular.cs");
      const regions = parseHtmlRegions(text);

      assert.equal(regions.length, 3, "expected 3 HtmlRegions");
      for (const r of regions) {
        assert.equal(r.form, "regular");
        assert.ok(r.bodyStart < r.bodyEnd, "body range must be non-empty");
        const body = text.slice(r.bodyStart, r.bodyEnd);
        assert.ok(
          body.includes("<") && body.includes(">"),
          `body should contain HTML tags, got: ${body.slice(0, 60)}`,
        );
      }
    });

    it("returns [] for negative/unmarked.cs (no markers or mis-placed markers)", () => {
      const text = FIXTURE("negative", "unmarked.cs");
      const regions = parseHtmlRegions(text);
      assert.deepEqual(regions, []);
    });

    it("returns [] for negative/interpolated.cs (FR-006 inertness)", () => {
      const text = FIXTURE("negative", "interpolated.cs");
      const regions = parseHtmlRegions(text);
      assert.deepEqual(regions, []);
    });
  });

  describe("US1 v2 — regular interpolated literals with /*lang=html*/ marker", () => {
    it("returns one interp-regular HtmlRegion per marker-annotated case in positive/interpolated-regular.cs", () => {
      const text = FIXTURE("positive", "interpolated-regular.cs");
      const regions = parseHtmlRegions(text);

      assert.equal(regions.length, 4, "expected 4 HtmlRegions (one per marker-annotated case)");
      for (const r of regions) {
        assert.equal(r.form, "interp-regular");
        assert.ok(r.bodyStart < r.bodyEnd, "body range must be non-empty");
        // bodyStart sits just after the opening `$"` sequence; bodyEnd points at the closing `"`.
        assert.equal(text[r.bodyStart - 1], '"', "char before bodyStart must be the opening quote");
        assert.equal(text[r.bodyStart - 2], "$", "char two before bodyStart must be the $ sigil");
        assert.equal(text[r.bodyEnd], '"', "bodyEnd must point at the closing quote");
      }
    });

    it("exposes the expected hole ranges per case (FR-004 at N=1)", () => {
      const text = FIXTURE("positive", "interpolated-regular.cs");
      const regions = parseHtmlRegions(text);
      assert.equal(regions.length, 4);

      // Case 1: single hole `{name}`.
      assert.equal(regions[0].holes.length, 1, "Case 1 should have exactly one hole");
      assert.equal(text.slice(regions[0].holes[0].start, regions[0].holes[0].end), "{name}");

      // Case 2: two holes `{url}` then `{label}`.
      assert.equal(regions[1].holes.length, 2, "Case 2 should have two holes");
      assert.equal(text.slice(regions[1].holes[0].start, regions[1].holes[0].end), "{url}");
      assert.equal(text.slice(regions[1].holes[1].start, regions[1].holes[1].end), "{label}");

      // Case 3: a single hole whose body contains a nested literal — the full
      // span `{(isOn ? "yes" : "no")}` is the hole (FR-007 / Principle I).
      assert.equal(regions[2].holes.length, 1, "Case 3 should have exactly one hole");
      assert.equal(
        text.slice(regions[2].holes[0].start, regions[2].holes[0].end),
        '{(isOn ? "yes" : "no")}',
      );

      // Case 4: single hole `{user}` adjacent to an escaped-quote attribute.
      assert.equal(regions[3].holes.length, 1, "Case 4 should have exactly one hole");
      assert.equal(text.slice(regions[3].holes[0].start, regions[3].holes[0].end), "{user}");
    });

    it("textSpansOf returns the ordered, contiguous complement of holes within each body", () => {
      const text = FIXTURE("positive", "interpolated-regular.cs");
      const regions = parseHtmlRegions(text);

      for (const r of regions) {
        const spans = textSpansOf(r);
        for (const s of spans) {
          assert.ok(s.start >= r.bodyStart, "span start must be within body");
          assert.ok(s.end <= r.bodyEnd, "span end must be within body");
          assert.ok(s.start < s.end, "span must be non-empty");
        }
        for (let i = 1; i < spans.length; i++) {
          assert.ok(spans[i - 1].end <= spans[i].start, "spans must be ordered, non-overlapping");
        }
        // Spans include the N-brace opener/closer runs; the body is tiled
        // by spans ∪ hole-inner-bodies rather than spans ∪ whole-holes.
        const innerBodies = r.holes.map((h) => ({ start: h.start + h.n, end: h.end - h.n }));
        const merged = [...spans, ...innerBodies].sort((a, b) => a.start - b.start);
        assert.equal(merged[0].start, r.bodyStart, "first range must start at bodyStart");
        assert.equal(merged[merged.length - 1].end, r.bodyEnd, "last range must end at bodyEnd");
        for (let i = 1; i < merged.length; i++) {
          assert.equal(merged[i - 1].end, merged[i].start, "ranges must be contiguous");
        }
      }
    });

    it("Case 3 nested literal inside hole does not terminate outer literal (FR-007, Principle I)", () => {
      const text = FIXTURE("positive", "interpolated-regular.cs");
      const regions = parseHtmlRegions(text);
      const r = regions[2];

      // The outer body must stretch all the way to `</p>` — i.e. the inner `"yes"`/`"no"` quotes
      // must NOT have prematurely closed the outer interpolated literal.
      const body = text.slice(r.bodyStart, r.bodyEnd);
      assert.ok(body.startsWith("<p>"), "outer body must start with <p>");
      assert.ok(body.endsWith("</p>"), "outer body must reach the closing </p>");

      // The inner `"yes"` and `"no"` literals must be fully contained inside the hole,
      // not leaking into a text span (which would get HTML-colored).
      const hole = r.holes[0];
      const holeText = text.slice(hole.start, hole.end);
      assert.ok(holeText.includes('"yes"'), "inner \"yes\" literal must live inside the hole");
      assert.ok(holeText.includes('"no"'), "inner \"no\" literal must live inside the hole");

      const spans = textSpansOf(r);
      for (const s of spans) {
        const sliced = text.slice(s.start, s.end);
        assert.ok(!sliced.includes('"yes"'), "no text span may contain the inner \"yes\" literal");
        assert.ok(!sliced.includes('"no"'), "no text span may contain the inner \"no\" literal");
      }
    });

    it("does not emit any inner HtmlRegion from inside a hole body (FR-007)", () => {
      const text = FIXTURE("positive", "interpolated-regular.cs");
      const regions = parseHtmlRegions(text);

      for (const outer of regions) {
        for (const hole of outer.holes) {
          for (const other of regions) {
            if (other === outer) continue;
            const overlaps =
              other.bodyStart < hole.end && other.bodyEnd > hole.start;
            assert.ok(!overlaps, "no region body may overlap a sibling region's hole");
          }
        }
      }
    });
  });

  describe("US2 v2 — verbatim and raw interpolated literals with /*lang=html*/ marker", () => {
    it("positive/interpolated-verbatim.cs: one interp-verbatim region per marker-annotated case", () => {
      const text = FIXTURE("positive", "interpolated-verbatim.cs");
      const regions = parseHtmlRegions(text);
      assert.equal(regions.length, 3, "expected 3 HtmlRegions");
      for (const r of regions) {
        assert.equal(r.form, "interp-verbatim");
        assert.ok(r.bodyStart < r.bodyEnd, "body range must be non-empty");
        assert.equal(text[r.bodyEnd], '"', "bodyEnd must point at the closing quote");
      }
      // Case 1: one hole `{name}`.
      assert.equal(regions[0].holes.length, 1);
      assert.equal(text.slice(regions[0].holes[0].start, regions[0].holes[0].end), "{name}");
      // Case 3: multi-line with two holes `{user}` and `{count}`.
      assert.equal(regions[2].holes.length, 2);
      assert.equal(text.slice(regions[2].holes[0].start, regions[2].holes[0].end), "{user}");
      assert.equal(text.slice(regions[2].holes[1].start, regions[2].holes[1].end), "{count}");
    });

    it("positive/interpolated-raw-n1.cs: one interp-raw-n1 region per case", () => {
      const text = FIXTURE("positive", "interpolated-raw-n1.cs");
      const regions = parseHtmlRegions(text);
      assert.equal(regions.length, 3, "expected 3 HtmlRegions");
      for (const r of regions) {
        assert.equal(r.form, "interp-raw-n1");
        assert.ok(r.bodyStart < r.bodyEnd);
      }
      // Case 1: `{name}` hole.
      assert.equal(regions[0].holes.length, 1);
      assert.equal(text.slice(regions[0].holes[0].start, regions[0].holes[0].end), "{name}");
      // Case 2: multi-line with three holes.
      assert.equal(regions[1].holes.length, 3);
    });

    it("positive/interpolated-raw-n2.cs: one interp-raw-n2 region per case with {{...}} holes", () => {
      const text = FIXTURE("positive", "interpolated-raw-n2.cs");
      const regions = parseHtmlRegions(text);
      assert.equal(regions.length, 3, "expected 3 HtmlRegions");
      for (const r of regions) {
        assert.equal(r.form, "interp-raw-n2");
      }
      // Case 1: `{{name}}` hole.
      assert.equal(regions[0].holes.length, 1);
      assert.equal(text.slice(regions[0].holes[0].start, regions[0].holes[0].end), "{{name}}");
      // Case 3: single `{` / `}` in text span are literal, not hole delimiters.
      assert.equal(regions[2].holes.length, 1);
      assert.equal(text.slice(regions[2].holes[0].start, regions[2].holes[0].end), "{{style}}");
    });

    it("positive/interpolated-raw-n3.cs: one interp-raw-n3 region per case with {{{...}}} holes", () => {
      const text = FIXTURE("positive", "interpolated-raw-n3.cs");
      const regions = parseHtmlRegions(text);
      assert.equal(regions.length, 2, "expected 2 HtmlRegions");
      for (const r of regions) {
        assert.equal(r.form, "interp-raw-n3");
      }
      // Case 1: `{{{name}}}` hole.
      assert.equal(regions[0].holes.length, 1);
      assert.equal(text.slice(regions[0].holes[0].start, regions[0].holes[0].end), "{{{name}}}");
    });

    it("negative/interpolated-raw-n4.cs: returns [] — N>=4 stays inert (FR-011)", () => {
      const text = FIXTURE("negative", "interpolated-raw-n4.cs");
      assert.deepEqual(parseHtmlRegions(text), []);
    });

    it("negative/marker-in-hole.cs: one outer region per case; inner marker does NOT promote (FR-007)", () => {
      const text = FIXTURE("negative", "marker-in-hole.cs");
      const regions = parseHtmlRegions(text);
      assert.equal(regions.length, 2, "expected exactly 2 outer regions (one per case)");
      for (const r of regions) {
        assert.equal(r.form, "interp-regular");
        assert.equal(r.holes.length, 1, "each outer region has one hole containing the inner literal");
        // The inner literal's body (with its own marker+literal) must be fully
        // contained within the outer hole — no inner HtmlRegion was emitted.
        const hole = r.holes[0];
        for (const other of regions) {
          if (other === r) continue;
          const overlaps = other.bodyStart < hole.end && other.bodyEnd > hole.start;
          assert.ok(!overlaps, "no outer region's body may overlap a sibling region's hole");
        }
      }
    });
  });

  describe("US2 — verbatim and raw non-interpolated literals", () => {
    it("detects all three cases in positive/verbatim.cs with form 'verbatim'", () => {
      const text = FIXTURE("positive", "verbatim.cs");
      const regions = parseHtmlRegions(text);

      assert.equal(regions.length, 3, "expected 3 HtmlRegions");
      for (const r of regions) {
        assert.equal(r.form, "verbatim");
        assert.ok(r.bodyStart < r.bodyEnd, "body range must be non-empty");
        const body = text.slice(r.bodyStart, r.bodyEnd);
        assert.ok(
          body.includes("<") && body.includes(">"),
          `body should contain HTML tags, got: ${body.slice(0, 60)}`,
        );
      }
    });

    it("detects all three cases in positive/raw.cs with form 'raw3'", () => {
      const text = FIXTURE("positive", "raw.cs");
      const regions = parseHtmlRegions(text);

      assert.equal(regions.length, 3, "expected 3 HtmlRegions");
      for (const r of regions) {
        assert.equal(r.form, "raw3");
        assert.ok(r.bodyStart < r.bodyEnd, "body range must be non-empty");
        const body = text.slice(r.bodyStart, r.bodyEnd);
        assert.ok(
          body.includes("<") && body.includes(">"),
          `body should contain HTML tags, got: ${body.slice(0, 60)}`,
        );
      }
    });

    it("returns [] for negative/raw-four-quote.cs (R3: N>3 raw literals unsupported)", () => {
      const text = FIXTURE("negative", "raw-four-quote.cs");
      assert.deepEqual(parseHtmlRegions(text), []);
    });
  });

  describe("US3 — varied marker forms", () => {
    it("accepts all seven variants in positive/markers-varied.cs", () => {
      const text = FIXTURE("positive", "markers-varied.cs");
      const regions = parseHtmlRegions(text);

      assert.equal(regions.length, 7, "expected 7 HtmlRegions");
      for (const r of regions) {
        assert.equal(r.form, "regular");
        assert.ok(r.bodyStart < r.bodyEnd, "body range must be non-empty");
        const body = text.slice(r.bodyStart, r.bodyEnd);
        assert.ok(
          body.includes("<") && body.includes(">"),
          `body should contain HTML tags, got: ${body.slice(0, 60)}`,
        );
      }
    });

    it("rejects every typo in negative/typo-marker.cs", () => {
      const text = FIXTURE("negative", "typo-marker.cs");
      assert.deepEqual(parseHtmlRegions(text), []);
    });
  });

  describe("US3 v2 — brace escapes and hole modifiers", () => {
    it("Case 1 (N=1 brace escape): {{ and }} are literal text — zero holes", () => {
      const text = FIXTURE("positive", "interpolated-braces-modifiers.cs");
      const regions = parseHtmlRegions(text);
      assert.ok(regions.length >= 1);
      const r = regions[0];
      assert.equal(r.form, "interp-regular");
      assert.equal(r.holes.length, 0, "{{ and }} must not produce holes");
      const body = text.slice(r.bodyStart, r.bodyEnd);
      assert.ok(body.includes("{{"), "body must contain the literal {{ text");
    });

    it("Case 2 (N=1 format specifier :F2): hole includes the :F2 clause", () => {
      const text = FIXTURE("positive", "interpolated-braces-modifiers.cs");
      const regions = parseHtmlRegions(text);
      assert.ok(regions.length >= 2);
      const r = regions[1];
      assert.equal(r.form, "interp-regular");
      assert.equal(r.holes.length, 1);
      assert.equal(text.slice(r.holes[0].start, r.holes[0].end), "{price:F2}");
    });

    it("Case 3 (N=1 alignment ,10): hole includes the ,10 clause", () => {
      const text = FIXTURE("positive", "interpolated-braces-modifiers.cs");
      const regions = parseHtmlRegions(text);
      assert.ok(regions.length >= 3);
      const r = regions[2];
      assert.equal(r.form, "interp-regular");
      assert.equal(r.holes.length, 1);
      assert.equal(text.slice(r.holes[0].start, r.holes[0].end), "{label,10}");
    });

    it("Case 4 (N=1 combined ,-5:X): hole includes the combined clause", () => {
      const text = FIXTURE("positive", "interpolated-braces-modifiers.cs");
      const regions = parseHtmlRegions(text);
      assert.ok(regions.length >= 4);
      const r = regions[3];
      assert.equal(r.form, "interp-regular");
      assert.equal(r.holes.length, 1);
      assert.equal(text.slice(r.holes[0].start, r.holes[0].end), "{value,-5:X}");
    });

    it("Case 5 (N=2 lone brace as text): single { / } do not open holes; {{name}} is the hole", () => {
      const text = FIXTURE("positive", "interpolated-braces-modifiers.cs");
      const regions = parseHtmlRegions(text);
      assert.ok(regions.length >= 5);
      const r = regions[4];
      assert.equal(r.form, "interp-raw-n2");
      assert.equal(r.holes.length, 1, "only {{name}} should be a hole");
      assert.equal(text.slice(r.holes[0].start, r.holes[0].end), "{{name}}");
      assert.equal(r.holes[0].n, 2);
    });

    it("Case 6 (N=3 run-of-1 brace as text): single { does not open hole; {{{name}}} is hole", () => {
      const text = FIXTURE("positive", "interpolated-braces-modifiers.cs");
      const regions = parseHtmlRegions(text);
      assert.ok(regions.length >= 6);
      const r = regions[5];
      assert.equal(r.form, "interp-raw-n3");
      assert.equal(r.holes.length, 1);
      assert.equal(text.slice(r.holes[0].start, r.holes[0].end), "{{{name}}}");
      assert.equal(r.holes[0].n, 3);
    });

    it("Case 7 (N=3 run-of-2 braces as text): {{ does not open hole; {{{name}}} is hole", () => {
      const text = FIXTURE("positive", "interpolated-braces-modifiers.cs");
      const regions = parseHtmlRegions(text);
      assert.ok(regions.length >= 7);
      const r = regions[6];
      assert.equal(r.form, "interp-raw-n3");
      assert.equal(r.holes.length, 1);
      assert.equal(text.slice(r.holes[0].start, r.holes[0].end), "{{{name}}}");
      assert.equal(r.holes[0].n, 3);
    });
  });

  describe("Polish (T058) — expanded negative corpus (SC-002/SC-003a)", () => {
    it("returns [] for negative/marker-not-adjacent.cs", () => {
      const text = FIXTURE("negative", "marker-not-adjacent.cs");
      assert.deepEqual(parseHtmlRegions(text), []);
    });

    it("returns [] for negative/marker-in-string.cs", () => {
      const text = FIXTURE("negative", "marker-in-string.cs");
      assert.deepEqual(parseHtmlRegions(text), []);
    });

    it("returns [] for negative/marker-before-non-string.cs", () => {
      const text = FIXTURE("negative", "marker-before-non-string.cs");
      assert.deepEqual(parseHtmlRegions(text), []);
    });
  });

  describe("v3 — Html() method trigger — non-interpolated", () => {
    it('Html("...") triggers a regular region with no holes', () => {
      const regions = parseHtmlRegions('Html("<p>hello</p>")');
      assert.equal(regions.length, 1);
      assert.equal(regions[0].form, "regular");
      assert.deepEqual(regions[0].holes, []);
      const body = 'Html("<p>hello</p>")'.slice(regions[0].bodyStart, regions[0].bodyEnd);
      assert.equal(body, "<p>hello</p>");
    });

    it('Html(@"...") triggers a verbatim region', () => {
      const regions = parseHtmlRegions('Html(@"<div>x</div>")');
      assert.equal(regions.length, 1);
      assert.equal(regions[0].form, "verbatim");
      assert.deepEqual(regions[0].holes, []);
    });

    it('Html("""...""") triggers a raw3 region', () => {
      const regions = parseHtmlRegions('Html("""<p>x</p>""")');
      assert.equal(regions.length, 1);
      assert.equal(regions[0].form, "raw3");
      assert.deepEqual(regions[0].holes, []);
    });

    it('Html  ("...") with spaces before paren still triggers', () => {
      const regions = parseHtmlRegions('Html  ("<p>x</p>")');
      assert.equal(regions.length, 1);
      assert.equal(regions[0].form, "regular");
    });

    it('Html\\n("...") with newline before paren still triggers', () => {
      const regions = parseHtmlRegions('Html\n("<p>x</p>")');
      assert.equal(regions.length, 1);
      assert.equal(regions[0].form, "regular");
    });

    it('myHtml("...") — word-boundary guard rejects suffix match', () => {
      assert.deepEqual(parseHtmlRegions('myHtml("<p>x</p>")'), []);
    });

    it('HtmlHelper("...") — word-boundary guard rejects prefix ident', () => {
      assert.deepEqual(parseHtmlRegions('HtmlHelper("<p>x</p>")'), []);
    });

    it('html("...") — case-sensitive: lowercase is rejected', () => {
      assert.deepEqual(parseHtmlRegions('html("<p>x</p>")'), []);
    });

    it('HTML("...") — case-sensitive: all-caps is rejected', () => {
      assert.deepEqual(parseHtmlRegions('HTML("<p>x</p>")'), []);
    });

    it('bare Html field without ( does not trigger', () => {
      assert.deepEqual(parseHtmlRegions('string Html = "<p>x</p>";'), []);
    });
  });

  describe("v3 — Html() method trigger — interpolated", () => {
    it('Html($"...{x}...") triggers interp-regular with one hole', () => {
      const src = 'Html($"<p>{name}</p>")';
      const regions = parseHtmlRegions(src);
      assert.equal(regions.length, 1);
      assert.equal(regions[0].form, "interp-regular");
      assert.equal(regions[0].holes.length, 1);
      assert.equal(regions[0].holes[0].n, 1);
      const hole = src.slice(regions[0].holes[0].start, regions[0].holes[0].end);
      assert.equal(hole, "{name}");
    });

    it('Html($@"...{x}...") triggers interp-verbatim', () => {
      const regions = parseHtmlRegions('Html($@"<p>{x}</p>")');
      assert.equal(regions.length, 1);
      assert.equal(regions[0].form, "interp-verbatim");
      assert.equal(regions[0].holes.length, 1);
    });

    it('Html($"""...{x}...""") triggers interp-raw-n1', () => {
      const regions = parseHtmlRegions('Html($"""<p>{x}</p>""")');
      assert.equal(regions.length, 1);
      assert.equal(regions[0].form, "interp-raw-n1");
      assert.equal(regions[0].holes[0].n, 1);
    });

    it('Html($$"""...{{x}}...""") triggers interp-raw-n2 with n=2 hole', () => {
      const regions = parseHtmlRegions('Html($$"""<p>{{x}}</p>""")');
      assert.equal(regions.length, 1);
      assert.equal(regions[0].form, "interp-raw-n2");
      assert.equal(regions[0].holes[0].n, 2);
      const src = 'Html($$"""<p>{{x}}</p>""")';
      const hole = src.slice(regions[0].holes[0].start, regions[0].holes[0].end);
      assert.equal(hole, "{{x}}");
    });

    it('Html($$$"""...{{{x}}}...""") triggers interp-raw-n3 with n=3 hole', () => {
      const regions = parseHtmlRegions('Html($$$"""<p>{{{x}}}</p>""")');
      assert.equal(regions.length, 1);
      assert.equal(regions[0].form, "interp-raw-n3");
      assert.equal(regions[0].holes[0].n, 3);
    });

    it('Html($$$$"""...""") N=4 is inert — no region produced (FR-007)', () => {
      assert.deepEqual(parseHtmlRegions('Html($$$$"""<p>{{{{x}}}}</p>""")'), []);
    });
  });

  describe("v3 — dual-trigger coexistence", () => {
    it('/*lang=html*/ Html("...") is idempotent — one region, not two', () => {
      const regions = parseHtmlRegions('/*lang=html*/ Html("<p>x</p>")');
      assert.equal(regions.length, 1);
      assert.equal(regions[0].form, "regular");
    });

    it('Html(/*lang=html*/"...") — method trigger fires, inner comment is opaque', () => {
      const regions = parseHtmlRegions('Html(/*lang=html*/"<p>x</p>")');
      assert.equal(regions.length, 1);
      assert.equal(regions[0].form, "regular");
    });

    it('file with both marker-triggered and method-triggered strings produces two regions', () => {
      const src = '/*lang=html*/$"<p>{x}</p>"\nHtml("<span>y</span>")';
      const regions = parseHtmlRegions(src);
      assert.equal(regions.length, 2);
      assert.equal(regions[0].form, "interp-regular");
      assert.equal(regions[1].form, "regular");
    });
  });

  describe("v6 — dotted receiver Html() trigger", () => {
    it('obj.Html("...") — simple identifier receiver triggers a regular region', () => {
      const src = 'obj.Html("<p>hello</p>")';
      const regions = parseHtmlRegions(src);
      assert.equal(regions.length, 1);
      assert.equal(regions[0].form, "regular");
      assert.deepEqual(regions[0].holes, []);
      assert.equal(src.slice(regions[0].bodyStart, regions[0].bodyEnd), "<p>hello</p>");
    });

    it('this.Html("...") — this keyword receiver triggers a regular region', () => {
      const regions = parseHtmlRegions('this.Html("<p>hello</p>")');
      assert.equal(regions.length, 1);
      assert.equal(regions[0].form, "regular");
    });

    it('MyClass.Html("...") — static class name receiver triggers a regular region', () => {
      const regions = parseHtmlRegions('MyClass.Html("<p>hello</p>")');
      assert.equal(regions.length, 1);
      assert.equal(regions[0].form, "regular");
    });

    it('GetBuilder().Html("...") — method-call-result receiver triggers a regular region', () => {
      const regions = parseHtmlRegions('GetBuilder().Html("<p>hello</p>")');
      assert.equal(regions.length, 1);
      assert.equal(regions[0].form, "regular");
    });

    it('a.b.Html("...") — multi-level chain receiver triggers a regular region', () => {
      const regions = parseHtmlRegions('a.b.Html("<p>hello</p>")');
      assert.equal(regions.length, 1);
      assert.equal(regions[0].form, "regular");
    });

    it('obj.Html($"...{x}...") — dotted receiver with interpolated string has one hole', () => {
      const src = 'obj.Html($"<p>{name}</p>")';
      const regions = parseHtmlRegions(src);
      assert.equal(regions.length, 1);
      assert.equal(regions[0].form, "interp-regular");
      assert.equal(regions[0].holes.length, 1);
      assert.equal(regions[0].holes[0].n, 1);
      assert.equal(src.slice(regions[0].holes[0].start, regions[0].holes[0].end), "{name}");
    });

    it('obj.Render("...") — wrong method name in dotted position produces no region', () => {
      assert.deepEqual(parseHtmlRegions('obj.Render("<p>x</p>")'), []);
    });

    it('obj.html("...") — lowercase in dotted position is rejected (case-sensitive)', () => {
      assert.deepEqual(parseHtmlRegions('obj.html("<p>x</p>")'), []);
    });
  });
});
