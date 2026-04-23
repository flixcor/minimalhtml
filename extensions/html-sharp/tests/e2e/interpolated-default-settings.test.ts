// SC-007 regression guard for v2: under default settings, marker-annotated
// interpolated literals emit one semantic token per text span (not per
// region) and zero tokens inside any hole's range. Populated in T017 (US1)
// and T042 (US2).
import * as assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import * as path from "node:path";
import * as vscode from "vscode";
import { parseHtmlRegions, textSpansOf } from "../../src/marker-parser";

describe("v2 interpolated SC-007 guard", () => {
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

  // Decode the 5-int-per-token stream into absolute [startOffset, endOffset)
  // byte ranges, resolving the delta-line/delta-char cursor the semantic
  // tokens API returns.
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

  async function assertOneTokenPerSpanZeroInHoles(fixture: string) {
    const { tokens, doc } = await openAndGetTokens(fixture);
    assert.ok(tokens, `provider must return SemanticTokens for ${fixture}`);

    const text = readFileSync(fixture, "utf8");
    const regions = parseHtmlRegions(text);
    const expectedSpans = regions.flatMap(textSpansOf);

    assert.ok(expectedSpans.length > 0, `fixture ${fixture} should yield text spans`);

    const tokenRanges = decodeTokenOffsets(tokens.data, doc);

    // Every emitted token must lie inside some text span. The provider splits
    // multi-line spans into per-line tokens (SemanticTokensBuilder does not
    // support multi-line tokens), so we check containment, not 1:1 counts.
    for (const t of tokenRanges) {
      const containing = expectedSpans.find((s) => s.start <= t.start && s.end >= t.end);
      assert.ok(containing, `token [${t.start},${t.end}) must lie inside a text span`);
    }

    // Every byte of every text span must be covered by some token (FR-003).
    for (const s of expectedSpans) {
      for (let offset = s.start; offset < s.end; offset++) {
        const ch = text.charCodeAt(offset);
        // Ignore EOL bytes — the provider's per-line splitter drops them.
        if (ch === 0x0a || ch === 0x0d) continue;
        const covering = tokenRanges.find((t) => t.start <= offset && t.end > offset);
        assert.ok(covering, `span byte @${offset} in [${s.start},${s.end}) must be covered by a token`);
      }
    }

    // No token may overlap any hole's INNER body (FR-003, FR-007). The hole's
    // N-brace opener/closer runs are intentionally INCLUDED in text spans so
    // the provider paints them too — see textSpansOf for the rationale.
    for (const r of regions) {
      for (const hole of r.holes) {
        const innerStart = hole.start + hole.n;
        const innerEnd = hole.end - hole.n;
        for (const t of tokenRanges) {
          const overlaps = t.start < innerEnd && t.end > innerStart;
          assert.ok(
            !overlaps,
            `token [${t.start},${t.end}) must not overlap hole inner body [${innerStart},${innerEnd})`,
          );
        }
      }
    }
  }

  it("US1: interpolated-regular.cs emits one token per text span, zero tokens inside holes", async () => {
    await assertOneTokenPerSpanZeroInHoles(
      path.join(fixturesRoot, "positive", "interpolated-regular.cs"),
    );
  });

  it("US2: interpolated-raw-n2.cs (N>1 representative) emits one token per text span, zero tokens inside holes", async () => {
    await assertOneTokenPerSpanZeroInHoles(
      path.join(fixturesRoot, "positive", "interpolated-raw-n2.cs"),
    );
  });
});
