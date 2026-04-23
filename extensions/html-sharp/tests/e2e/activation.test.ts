import * as assert from "node:assert/strict";
import * as path from "node:path";
import * as vscode from "vscode";

describe("extension activation", () => {
  const extensionId = "felixcornelissen.minimalhtml-extension";
  const fixture = path.resolve(
    __dirname,
    "..",
    "..",
    "..",
    "tests",
    "fixtures",
    "positive",
    "regular.cs",
  );

  it("is installed in the test VS Code instance", () => {
    const ext = vscode.extensions.getExtension(extensionId);
    assert.ok(ext, "minimalhtml-extension should be present in the Extension Host");
  });

  it("activates on first .cs file open (onLanguage:csharp)", async () => {
    const ext = vscode.extensions.getExtension(extensionId);
    assert.ok(ext, "extension must be discoverable before activation check");

    const doc = await vscode.workspace.openTextDocument(vscode.Uri.file(fixture));
    await vscode.window.showTextDocument(doc);
    assert.equal(doc.languageId, "csharp", "fixture must open as csharp, not plaintext");

    for (let i = 0; i < 50 && !ext.isActive; i++) {
      await new Promise((r) => setTimeout(r, 100));
    }
    assert.equal(ext.isActive, true, "extension must be active after opening a .cs file");
  });
});
