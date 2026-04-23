import * as assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import * as path from "node:path";
import { parseHtmlRegions, textSpansOf } from "../../src/marker-parser";

// The provider itself imports the `vscode` module which is only available at
// runtime inside the Extension Host. In unit tests we exercise the contract
// the provider relies on — parseHtmlRegions returning non-overlapping,
// monotonically increasing byte ranges suitable for SemanticTokensBuilder.
// Full token-emission behavior is asserted in tests/e2e/.

const REPO_ROOT = path.resolve(__dirname, "..", "..", "..");
const FIXTURE = (...p: string[]) =>
  readFileSync(path.resolve(REPO_ROOT, "tests", "fixtures", ...p), "utf8");

describe("semantic-tokens-provider (pure-Node surface)", () => {
  it("delegates to parseHtmlRegions for region detection", () => {
    assert.equal(typeof parseHtmlRegions, "function");
    assert.deepEqual(parseHtmlRegions(""), []);
  });

  it("parser output satisfies SemanticTokensBuilder contract (monotonic, non-overlapping)", () => {
    const text = FIXTURE("positive", "regular.cs");
    const regions = parseHtmlRegions(text);

    assert.ok(regions.length > 0, "fixture should produce regions");

    let prevEnd = -1;
    for (const r of regions) {
      assert.ok(r.bodyStart >= prevEnd, "regions must be monotonically increasing");
      assert.ok(r.bodyEnd > r.bodyStart, "body range must be non-empty");
      prevEnd = r.bodyEnd;
    }
  });

  it("emits zero regions for unmarked fixture (provider would emit zero tokens)", () => {
    const text = FIXTURE("negative", "unmarked.cs");
    assert.deepEqual(parseHtmlRegions(text), []);
  });

  describe("US2 — verbatim and raw contract", () => {
    it("verbatim fixture yields monotonic, non-overlapping ranges", () => {
      const text = FIXTURE("positive", "verbatim.cs");
      const regions = parseHtmlRegions(text);

      assert.ok(regions.length > 0, "fixture should produce regions");

      let prevEnd = -1;
      for (const r of regions) {
        assert.ok(r.bodyStart >= prevEnd, "regions must be monotonically increasing");
        assert.ok(r.bodyEnd > r.bodyStart, "body range must be non-empty");
        prevEnd = r.bodyEnd;
      }
    });

    it("raw fixture yields monotonic, non-overlapping ranges", () => {
      const text = FIXTURE("positive", "raw.cs");
      const regions = parseHtmlRegions(text);

      assert.ok(regions.length > 0, "fixture should produce regions");

      let prevEnd = -1;
      for (const r of regions) {
        assert.ok(r.bodyStart >= prevEnd, "regions must be monotonically increasing");
        assert.ok(r.bodyEnd > r.bodyStart, "body range must be non-empty");
        prevEnd = r.bodyEnd;
      }
    });

    it("emits zero regions for raw-four-quote fixture (provider would emit zero tokens)", () => {
      const text = FIXTURE("negative", "raw-four-quote.cs");
      assert.deepEqual(parseHtmlRegions(text), []);
    });
  });

  // Per contracts/semantic-tokens.md §2.3: for interpolated HtmlRegions, the
  // provider emits one `embeddedHtml` token per TextSpan (not per region) and
  // zero tokens inside every hole's range. These assertions exercise the
  // pure-Node contract the provider consumes: `parseHtmlRegions(...).flatMap(textSpansOf)`.
  describe("US1 v2 — per-text-span emission contract", () => {
    it("yields one text span per region for v1 (non-interpolated) fixtures (byte-for-byte v1 parity)", () => {
      const text = FIXTURE("positive", "regular.cs");
      const regions = parseHtmlRegions(text);
      assert.ok(regions.length > 0, "fixture should produce regions");

      for (const r of regions) {
        const spans = textSpansOf(r);
        assert.equal(spans.length, 1, "non-interpolated region must yield exactly one text span");
        assert.equal(spans[0].start, r.bodyStart, "span start must equal bodyStart");
        assert.equal(spans[0].end, r.bodyEnd, "span end must equal bodyEnd");
      }
    });

    it("yields N+1 text spans per interp-regular region with N holes (Case 1: 2, Case 2: 3, Case 3: 2, Case 4: 2)", () => {
      const text = FIXTURE("positive", "interpolated-regular.cs");
      const regions = parseHtmlRegions(text);
      assert.equal(regions.length, 4, "expected 4 interp-regular regions");

      const expectedSpanCounts = [2, 3, 2, 2];
      for (let i = 0; i < regions.length; i++) {
        const spans = textSpansOf(regions[i]);
        assert.equal(
          spans.length,
          expectedSpanCounts[i],
          `Case ${i + 1} should yield ${expectedSpanCounts[i]} text spans (N+1 for N holes)`,
        );
      }
    });

    it("produces globally monotonic, non-overlapping text-span ranges across all regions", () => {
      const text = FIXTURE("positive", "interpolated-regular.cs");
      const regions = parseHtmlRegions(text);
      const allSpans = regions.flatMap(textSpansOf);

      assert.ok(allSpans.length > 0, "fixture should yield at least one text span");

      let prevEnd = -1;
      for (const s of allSpans) {
        assert.ok(s.start >= prevEnd, "spans must be monotonically increasing across regions");
        assert.ok(s.end > s.start, "span must be non-empty");
        prevEnd = s.end;
      }
    });

    it("no text span overlaps any hole's inner body (FR-003, FR-007)", () => {
      const text = FIXTURE("positive", "interpolated-regular.cs");
      const regions = parseHtmlRegions(text);

      for (const r of regions) {
        const spans = textSpansOf(r);
        for (const span of spans) {
          for (const hole of r.holes) {
            // Spans include N-brace opener/closer runs; the inner body is the
            // C# expression between them — that is what must stay uncovered.
            const innerStart = hole.start + hole.n;
            const innerEnd = hole.end - hole.n;
            const overlaps = span.start < innerEnd && span.end > innerStart;
            assert.ok(
              !overlaps,
              `text span [${span.start},${span.end}) must not overlap hole inner body [${innerStart},${innerEnd})`,
            );
          }
        }
      }
    });
  });

  // Per contracts/semantic-tokens.md §2.3: every (form × N) pair of
  // marker-annotated interpolated literal must yield one token per text span
  // with zero tokens inside holes, and N>=4 raw stays inert (FR-011).
  describe("US2 v2 — per-text-span emission across all (form × N) pairs", () => {
    const positiveFixtures = [
      "interpolated-verbatim.cs",
      "interpolated-raw-n1.cs",
      "interpolated-raw-n2.cs",
      "interpolated-raw-n3.cs",
    ];

    for (const name of positiveFixtures) {
      it(`positive/${name}: spans monotonic, non-overlapping, none overlaps a hole`, () => {
        const text = FIXTURE("positive", name);
        const regions = parseHtmlRegions(text);
        assert.ok(regions.length > 0, `${name} should produce regions`);

        const allSpans = regions.flatMap(textSpansOf);
        let prevEnd = -1;
        for (const s of allSpans) {
          assert.ok(s.start >= prevEnd, "spans must be monotonically increasing");
          assert.ok(s.end > s.start, "span must be non-empty");
          prevEnd = s.end;
        }

        for (const r of regions) {
          const spans = textSpansOf(r);
          assert.equal(
            spans.length,
            r.holes.length + 1,
            `interpolated region must yield N+1 text spans for N holes (got ${spans.length} spans, ${r.holes.length} holes)`,
          );
          for (const span of spans) {
            for (const hole of r.holes) {
              // Spans include the N-brace opener/closer runs but must not
              // overlap the hole's INNER body — see textSpansOf rationale.
              const innerStart = hole.start + hole.n;
              const innerEnd = hole.end - hole.n;
              const overlaps = span.start < innerEnd && span.end > innerStart;
              assert.ok(!overlaps, `span must not overlap hole inner body in ${name}`);
            }
          }
        }
      });
    }

    it("negative/interpolated-raw-n4.cs: yields zero regions (FR-011)", () => {
      const text = FIXTURE("negative", "interpolated-raw-n4.cs");
      assert.deepEqual(parseHtmlRegions(text), []);
    });
  });
});
