import * as assert from "node:assert/strict";
import * as path from "node:path";
import * as vscode from "vscode";

// SC-005 / SC-006: re-opening a previously-opened .cs file must produce the
// same HTML scopes at the same offsets as the first open. This asserts the
// provider is deterministic across editor lifecycle events and the injection
// grammar re-registers correctly when the document is restored.
// Extended in T061 (US2) to cover interpolated literals (SC-006 v2 forms).

const FIXTURES_ROOT = path.resolve(__dirname, "..", "..", "..", "tests", "fixtures", "positive");

async function openAndCollectTokens(fixture: string): Promise<Uint32Array> {
  const doc = await vscode.workspace.openTextDocument(vscode.Uri.file(fixture));
  await vscode.window.showTextDocument(doc, { preview: false });
  const tokens = await vscode.commands.executeCommand<vscode.SemanticTokens>(
    "vscode.provideDocumentSemanticTokens",
    doc.uri,
  );
  assert.ok(tokens, `provider must return SemanticTokens for ${fixture}`);
  return tokens.data;
}

async function assertReopenParity(fixture: string) {
  const first = await openAndCollectTokens(fixture);
  await vscode.commands.executeCommand("workbench.action.closeActiveEditor");
  const second = await openAndCollectTokens(fixture);
  assert.deepEqual(
    Array.from(second),
    Array.from(first),
    `reopened document must emit identical semantic tokens for ${fixture}`,
  );
}

describe("SC-005 / SC-006: reopen preserves coloring", () => {
  it("v1: regular.cs produces identical tokens on second open", async () => {
    await assertReopenParity(path.join(FIXTURES_ROOT, "regular.cs"));
  });

  it("v2 SC-006: interpolated-regular.cs produces identical tokens on second open", async () => {
    await assertReopenParity(path.join(FIXTURES_ROOT, "interpolated-regular.cs"));
  });

  it("v2 SC-006: interpolated-raw-n2.cs produces identical tokens on second open (N>1 representative)", async () => {
    await assertReopenParity(path.join(FIXTURES_ROOT, "interpolated-raw-n2.cs"));
  });
});
