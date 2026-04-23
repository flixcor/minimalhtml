// v2 load-bearing cross-layer test — asserts that for every positive
// interpolated fixture, the parser's text spans (the authoritative source
// for semantic-token emission) are COVERED by the TextMate grammar's outer-
// block-scope (`meta.embedded.block.html.cs`) ranges.
//
// Byte-for-byte equality is not achievable: when a hole opens inside an
// HTML attribute context (e.g. `href="{url}"`), text.html.basic's tag-
// scanner owns `{` before the outer grammar can break the text span. The
// grammar therefore over-approximates — it may cover a hole's bytes too.
// That is acceptable because the semantic-tokens provider uses the parser
// (not the grammar) to decide where tokens are emitted; the grammar's
// scope only governs VS Code's embedded-language integration.
//
// Populated incrementally: T016 (US1), T041 (US2), T056 (US3).
import * as assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import * as path from "node:path";
import { parseHtmlRegions, textSpansOf } from "../../src/marker-parser";
import { outerEmbeddedScopeRanges } from "./grammar-loader";

const REPO_ROOT = path.resolve(__dirname, "..", "..", "..");
const FIXTURE = (...p: string[]) =>
  readFileSync(path.resolve(REPO_ROOT, "tests", "fixtures", ...p), "utf8");

describe("grammar-parser-sync (v2)", () => {
  it("suite stub is wired (populated incrementally per user story)", () => {
    assert.ok(true);
  });

  const coversTestCase = (fixtureRel: string[]) => {
    it(`${fixtureRel.join("/")}: every parser text span lies inside some meta.embedded.block.html.cs range`, async () => {
      const text = FIXTURE(...fixtureRel);

      const regions = parseHtmlRegions(text);
      const parserSpans = regions.flatMap(textSpansOf);
      const grammarRanges = await outerEmbeddedScopeRanges(text);

      assert.ok(
        parserSpans.length > 0,
        "parser must produce at least one text span for the positive fixture",
      );
      assert.ok(
        grammarRanges.length > 0,
        "grammar must emit at least one meta.embedded.block.html.cs range for the positive fixture",
      );

      for (const span of parserSpans) {
        const covering = grammarRanges.find(
          (r) => r.start <= span.start && r.end >= span.end,
        );
        assert.ok(
          covering,
          `parser text span [${span.start}, ${span.end}) must be covered by some grammar range; grammar ranges: ${JSON.stringify(grammarRanges)}`,
        );
      }
    });
  };

  describe("US1 — interp-regular text spans are covered by grammar outer scope ranges", () => {
    coversTestCase(["positive", "interpolated-regular.cs"]);
  });

  describe("US2 — verbatim and raw interpolated (N=1/2/3) text spans are covered by grammar outer scope ranges", () => {
    coversTestCase(["positive", "interpolated-verbatim.cs"]);
    coversTestCase(["positive", "interpolated-raw-n1.cs"]);
    coversTestCase(["positive", "interpolated-raw-n2.cs"]);
    coversTestCase(["positive", "interpolated-raw-n3.cs"]);
  });

  describe("US3 — brace-escape and modifier fixtures text spans are covered by grammar outer scope ranges", () => {
    coversTestCase(["positive", "interpolated-braces-modifiers.cs"]);
  });
});

describe("grammar-parser-sync (v3)", () => {
  it("suite stub is wired (populated when method-trigger grammar entries land)", () => {
    assert.ok(true);
  });

  const REPO_ROOT_V3 = path.resolve(__dirname, "..", "..", "..");
  const FIXTURE_V3 = (...p: string[]) =>
    readFileSync(path.resolve(REPO_ROOT_V3, "tests", "fixtures", ...p), "utf8");

  const coversTestCaseV3 = (fixtureRel: string[]) => {
    it(`${fixtureRel.join("/")}: every parser text span lies inside some meta.embedded.block.html.cs range`, async () => {
      const text = FIXTURE_V3(...fixtureRel);

      const regions = parseHtmlRegions(text);
      const parserSpans = regions.flatMap(textSpansOf);
      const grammarRanges = await outerEmbeddedScopeRanges(text);

      assert.ok(
        parserSpans.length > 0,
        "parser must produce at least one text span for the positive fixture",
      );
      assert.ok(
        grammarRanges.length > 0,
        "grammar must emit at least one meta.embedded.block.html.cs range for the positive fixture",
      );

      for (const span of parserSpans) {
        const covering = grammarRanges.find(
          (r) => r.start <= span.start && r.end >= span.end,
        );
        assert.ok(
          covering,
          `parser text span [${span.start}, ${span.end}) must be covered by some grammar range; grammar ranges: ${JSON.stringify(grammarRanges)}`,
        );
      }
    });
  };

  describe("US1 v3 — method-trigger-regular.cs text spans are covered by grammar outer scope ranges", () => {
    coversTestCaseV3(["positive", "method-trigger-regular.cs"]);
  });

  describe("US3 v3 — method-trigger-interp.cs text spans are covered by grammar outer scope ranges", () => {
    coversTestCaseV3(["positive", "method-trigger-interp.cs"]);
  });
});
