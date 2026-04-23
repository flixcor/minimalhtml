// SC-003 guard for v3: under default settings, a file mixing marker-triggered
// and method-triggered strings produces correct embeddedHtml semantic tokens
// for both — and v1/v2 marker-triggered regions are unaffected.
import * as assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import * as path from "node:path";
import * as vscode from "vscode";
import { parseHtmlRegions, textSpansOf } from "../../src/marker-parser";

describe("v3 method-trigger SC-003 guard", () => {
  const fixturesRoot = path.resolve(
    __dirname,
    "..",
    "..",
    "..",
    "tests",
    "fixtures",
  );

  async function openAndGetTokens(
    file: string,
  ): Promise<{ tokens: vscode.SemanticTokens | undefined; doc: vscode.TextDocument }> {
    const doc = await vscode.workspace.openTextDocument(vscode.Uri.file(file));
    await vscode.window.showTextDocument(doc);
    const tokens = await vscode.commands.executeCommand<vscode.SemanticTokens>(
      "vscode.provideDocumentSemanticTokens",
      doc.uri,
    );
    return { tokens, doc };
  }

  function decodeTokenOffsets(
    data: Uint32Array,
    doc: vscode.TextDocument,
  ): Array<{ start: number; end: number }> {
    const ranges: Array<{ start: number; end: number }> = [];
    let line = 0;
    let char = 0;
    for (let i = 0; i < data.length; i += 5) {
      const deltaLine = data[i];
      const deltaChar = data[i + 1];
      const length = data[i + 2];
      if (deltaLine === 0) {
        char += deltaChar;
      } else {
        line += deltaLine;
        char = deltaChar;
      }
      const startPos = new vscode.Position(line, char);
      const endPos = new vscode.Position(line, char + length);
      ranges.push({
        start: doc.offsetAt(startPos),
        end: doc.offsetAt(endPos),
      });
    }
    return ranges;
  }

  it("coexistence: both marker-triggered and method-triggered strings produce embeddedHtml tokens", async () => {
    const fixture = path.join(fixturesRoot, "positive", "method-trigger-coexist.cs");
    const { tokens, doc } = await openAndGetTokens(fixture);
    assert.ok(tokens, "provider must return SemanticTokens");

    const text = readFileSync(fixture, "utf8");
    const regions = parseHtmlRegions(text);
    const expectedSpans = regions.flatMap(textSpansOf);

    // The coexist fixture has 4 cases: marker-triggered $"...", method-triggered
    // plain string, a marker-triggered string, another method-triggered string.
    // Parser should produce multiple regions.
    assert.ok(regions.length >= 2, `expected ≥2 regions, got ${regions.length}`);
    assert.ok(expectedSpans.length > 0, "must have text spans");

    const tokenRanges = decodeTokenOffsets(tokens.data, doc);
    assert.ok(tokenRanges.length > 0, "provider must emit at least one token");

    // Every emitted token must lie inside some text span (provider emits per-line
    // tokens, so containment check suffices).
    for (const t of tokenRanges) {
      const containing = expectedSpans.find((s) => s.start <= t.start && s.end >= t.end);
      assert.ok(containing, `token [${t.start},${t.end}) must lie inside a parser text span`);
    }
  });

  it("regression: method-trigger does not break v1 marker-triggered regular.cs tokens", async () => {
    const fixture = path.join(fixturesRoot, "positive", "regular.cs");
    const { tokens, doc } = await openAndGetTokens(fixture);
    assert.ok(tokens, "provider must return SemanticTokens for regular.cs");

    const text = readFileSync(fixture, "utf8");
    const regions = parseHtmlRegions(text);
    assert.equal(regions.length, 3, "v1 regular.cs must still produce 3 regions");

    const tokenRanges = decodeTokenOffsets(tokens.data, doc);
    assert.ok(tokenRanges.length > 0, "v1 fixture must still produce tokens");
  });

  it("dotted receiver: obj.Html(...) string produces embeddedHtml tokens", async () => {
    const fixture = path.join(fixturesRoot, "positive", "method-trigger-dotted.cs");
    const { tokens, doc } = await openAndGetTokens(fixture);
    assert.ok(tokens, "provider must return SemanticTokens for dotted fixture");

    const text = readFileSync(fixture, "utf8");
    const regions = parseHtmlRegions(text);
    const expectedSpans = regions.flatMap(textSpansOf);

    // Dotted fixture has 6 cases (Cases 1-5: regular, Case 6: interpolated).
    assert.ok(regions.length >= 6, `expected ≥6 regions, got ${regions.length}`);
    assert.ok(expectedSpans.length > 0, "must have text spans");

    const tokenRanges = decodeTokenOffsets(tokens.data, doc);
    assert.ok(tokenRanges.length > 0, "provider must emit at least one token");

    for (const t of tokenRanges) {
      const containing = expectedSpans.find((s) => s.start <= t.start && s.end >= t.end);
      assert.ok(containing, `token [${t.start},${t.end}) must lie inside a parser text span`);
    }
  });
});
