import * as assert from "node:assert/strict";
import * as path from "node:path";
import * as vscode from "vscode";

// SC-006 regression guard: under VS Code's default settings
// (editor.semanticHighlighting.enabled === true), our provider must emit a
// `string` semantic token over each HtmlRegion body. Without this, the HTML
// TextMate scopes from the injection grammar would be visually masked by
// Roslyn's flat string semantic token.
describe("SC-006: HTML coloring under default settings", () => {
  const fixturesRoot = path.resolve(
    __dirname,
    "..",
    "..",
    "..",
    "tests",
    "fixtures",
  );
  const positiveFixture = path.join(fixturesRoot, "positive", "regular.cs");
  const unmarkedFixture = path.join(fixturesRoot, "negative", "unmarked.cs");
  const interpolatedFixture = path.join(fixturesRoot, "negative", "interpolated.cs");

  async function openAndGetTokens(
    file: string,
  ): Promise<vscode.SemanticTokens | undefined> {
    const doc = await vscode.workspace.openTextDocument(vscode.Uri.file(file));
    await vscode.window.showTextDocument(doc);
    return await vscode.commands.executeCommand<vscode.SemanticTokens>(
      "vscode.provideDocumentSemanticTokens",
      doc.uri,
    );
  }

  it("emits one semantic token per marker-annotated regular literal", async () => {
    const tokens = await openAndGetTokens(positiveFixture);
    assert.ok(tokens, "provider must return SemanticTokens for positive fixture");
    // Token stream is 5 ints per token (deltaLine, deltaChar, length, type, mod).
    // positive/regular.cs has 3 marker-annotated cases => 3 tokens.
    assert.equal(
      tokens.data.length,
      3 * 5,
      `expected 15 ints (3 tokens × 5), got ${tokens.data.length}`,
    );
  });

  it("emits zero semantic tokens for unmarked literals (no false positives)", async () => {
    const tokens = await openAndGetTokens(unmarkedFixture);
    const len = tokens?.data?.length ?? 0;
    assert.equal(len, 0, "unmarked literals must not trigger the provider");
  });

  it("emits zero semantic tokens for UNMARKED interpolated literals (v2: marker is required)", async () => {
    // fixtures/negative/interpolated.cs was repurposed in v2 T018: it now
    // contains only UNMARKED $"..." / $@"..." / $"""...""" cases — marked
    // interpolated literals are POSITIVE under v2 and covered by
    // tests/e2e/interpolated-default-settings.test.ts.
    const tokens = await openAndGetTokens(interpolatedFixture);
    const len = tokens?.data?.length ?? 0;
    assert.equal(len, 0, "unmarked interpolated literals must not trigger the provider");
  });
});
