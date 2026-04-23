import * as assert from "node:assert/strict";
import * as fs from "node:fs";
import * as path from "node:path";
import { parseHtmlRegions } from "../../src/marker-parser";
import { validateHtmlRegions } from "../../src/html-validator";

const REPO_ROOT = path.resolve(__dirname, "..", "..", "..");

describe("validateHtmlRegions", () => {
  // T004 — Phase 2 foundational tests (inline)

  it("(a) unclosed <p> — returns one diagnostic", () => {
    const text = 'var x = /*lang=html*/ "<p>text";';
    const regions = parseHtmlRegions(text);
    const diags = validateHtmlRegions(regions, text);
    assert.strictEqual(diags.length, 1);
    const d = diags[0];
    assert.ok(d.message.includes("p"), `expected message to mention 'p', got: ${d.message}`);
    const region = regions[0];
    assert.ok(
      d.startOffset >= region.bodyStart && d.startOffset < region.bodyEnd,
      `startOffset ${d.startOffset} must be inside [${region.bodyStart}, ${region.bodyEnd})`,
    );
  });

  it("(b) properly closed <p> — returns empty array", () => {
    const text = 'var x = /*lang=html*/ "<p>text</p>";';
    const regions = parseHtmlRegions(text);
    const diags = validateHtmlRegions(regions, text);
    assert.deepEqual(diags, []);
  });

  it("(c) void element <br> — returns empty array", () => {
    const text = 'var x = /*lang=html*/ "<br>";';
    const regions = parseHtmlRegions(text);
    const diags = validateHtmlRegions(regions, text);
    assert.deepEqual(diags, []);
  });

  it("(d) empty region body — returns empty array", () => {
    const text = 'var x = /*lang=html*/ "";';
    const regions = parseHtmlRegions(text);
    const diags = validateHtmlRegions(regions, text);
    assert.deepEqual(diags, []);
  });

  // T007 — US1: fixture-based test using diagnostics-unclosed.cs

  it("[US1] diagnostics-unclosed.cs fixture — correct diagnostics per region", () => {
    const fixturePath = path.resolve(REPO_ROOT, "tests", "fixtures", "positive", "diagnostics-unclosed.cs");
    const text = fs.readFileSync(fixturePath, "utf-8");
    const regions = parseHtmlRegions(text);
    const diags = validateHtmlRegions(regions, text);

    // Region 0: unclosed <p> inside <div> → 1 diagnostic
    // Region 1: unclosed <section> → 1 diagnostic
    // Region 2: valid <span></span> → 0 diagnostics
    assert.strictEqual(diags.length, 2, `expected 2 diagnostics, got ${diags.length}`);

    const msgs = diags.map((d) => d.message.toLowerCase());
    assert.ok(msgs.some((m) => m.includes("p")), "expected diagnostic for unclosed <p>");
    assert.ok(msgs.some((m) => m.includes("section")), "expected diagnostic for unclosed <section>");

    for (const d of diags) {
      const enclosingRegion = regions.find(
        (r) => d.startOffset >= r.bodyStart && d.startOffset < r.bodyEnd,
      );
      assert.ok(
        enclosingRegion !== undefined,
        `diagnostic startOffset ${d.startOffset} must fall inside a region`,
      );
    }
  });

  // T011 — US2: valid interpolated HTML produces zero diagnostics
  it("[US2] diagnostics-valid-interp.cs fixture — zero diagnostics", () => {
    const fixturePath = path.resolve(REPO_ROOT, "tests", "fixtures", "negative", "diagnostics-valid-interp.cs");
    const text = fs.readFileSync(fixturePath, "utf-8");
    const regions = parseHtmlRegions(text);
    const diags = validateHtmlRegions(regions, text);
    assert.deepEqual(diags, [], `expected zero diagnostics, got: ${JSON.stringify(diags)}`);
  });

  // T012 — US2: inline interpolated HTML with holes in attribute values
  it("[US2] inline interpolated HTML with holes — zero diagnostics", () => {
    const text = 'var x = $"<div class=\'{x}\'><span>{y}</span></div>";';
    const regions = parseHtmlRegions(text);
    const diags = validateHtmlRegions(regions, text);
    assert.deepEqual(diags, [], `expected zero diagnostics, got: ${JSON.stringify(diags)}`);
  });
});
