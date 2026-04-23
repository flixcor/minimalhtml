import * as vscode from "vscode";
import { ParseCache } from "./parse-cache";
import { validateHtmlRegions, HtmlDiagnostic } from "./html-validator";

export class HtmlDiagnosticsProvider {
  constructor(
    private cache: ParseCache,
    private collection: vscode.DiagnosticCollection,
  ) {}

  validate(document: vscode.TextDocument): void {
    if (document.languageId !== "csharp") return;

    const enabled = vscode.workspace
      .getConfiguration("minimalhtml")
      .get<boolean>("diagnostics.enabled", true);
    if (!enabled) {
      this.collection.delete(document.uri);
      return;
    }

    const regions = this.cache.getRegions(document);
    const raw = validateHtmlRegions(regions, document.getText());
    const diagnostics = raw.map((d: HtmlDiagnostic) => {
      const diag = new vscode.Diagnostic(
        new vscode.Range(
          document.positionAt(d.startOffset),
          document.positionAt(d.endOffset),
        ),
        d.message,
        vscode.DiagnosticSeverity.Error,
      );
      diag.source = "minimalhtml";
      return diag;
    });
    this.collection.set(document.uri, diagnostics);
  }

  validateAll(documents: readonly vscode.TextDocument[]): void {
    for (const doc of documents) {
      this.validate(doc);
    }
  }

  clear(document: vscode.TextDocument): void {
    this.collection.delete(document.uri);
  }

  clearAll(): void {
    this.collection.clear();
  }
}
