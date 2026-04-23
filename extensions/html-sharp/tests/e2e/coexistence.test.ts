import * as assert from "node:assert/strict";
import * as path from "node:path";
import * as vscode from "vscode";

// T062 / FR-011: coexistence with ms-dotnettools.csharp.
//
// The test-electron host runs with --disable-extensions, so ms-dotnettools.csharp
// is NOT installed in the test VS Code instance. We therefore cannot directly
// observe token parity between "both extensions installed" and "csharp only".
// What we CAN prove here is the necessary-and-sufficient condition: our
// provider MUST emit zero semantic tokens across every fixture that contains
// no marker. If that invariant holds, then installing ms-dotnettools.csharp
// alongside us cannot produce a different rendered token stream than
// ms-dotnettools.csharp alone, because our contribution is provably empty.
// (See `contracts/semantic-tokens.md` §3: the provider emits no tokens
// outside HtmlRegions.)
describe("T062: provider contributes zero tokens outside HtmlRegions (coexistence precondition)", () => {
  const fixturesRoot = path.resolve(
    __dirname,
    "..",
    "..",
    "..",
    "tests",
    "fixtures",
  );
  const unmarkedFixtures = [
    path.join(fixturesRoot, "negative", "unmarked.cs"),
    path.join(fixturesRoot, "negative", "interpolated.cs"),
    path.join(fixturesRoot, "negative", "typo-marker.cs"),
    path.join(fixturesRoot, "negative", "raw-four-quote.cs"),
    path.join(fixturesRoot, "negative", "marker-not-adjacent.cs"),
    path.join(fixturesRoot, "negative", "marker-in-string.cs"),
    path.join(fixturesRoot, "negative", "marker-before-non-string.cs"),
  ];

  for (const fixture of unmarkedFixtures) {
    const name = path.basename(fixture);
    it(`emits zero semantic tokens for ${name}`, async () => {
      const doc = await vscode.workspace.openTextDocument(vscode.Uri.file(fixture));
      await vscode.window.showTextDocument(doc, { preview: false });
      const tokens = await vscode.commands.executeCommand<vscode.SemanticTokens>(
        "vscode.provideDocumentSemanticTokens",
        doc.uri,
      );
      const len = tokens?.data?.length ?? 0;
      assert.equal(
        len,
        0,
        `${name}: provider must emit zero tokens (got ${len / 5} tokens)`,
      );
    });
  }
});
