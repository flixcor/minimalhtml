import * as vscode from "vscode";
import { CsharpHtmlSemanticTokensProvider, legend } from "./semantic-tokens-provider";
import { HtmlHoverProvider } from "./hover-provider";
import { HtmlCompletionProvider } from "./completion-provider";
import { ParseCache } from "./parse-cache";
import { HtmlDiagnosticsProvider } from "./diagnostics-provider";

export function activate(context: vscode.ExtensionContext): void {
  const cache = new ParseCache();

  const diagnosticCollection = vscode.languages.createDiagnosticCollection("minimalhtml");
  context.subscriptions.push(diagnosticCollection);

  const diagnosticsProvider = new HtmlDiagnosticsProvider(cache, diagnosticCollection);
  diagnosticsProvider.validateAll(vscode.workspace.textDocuments);

  context.subscriptions.push(
    vscode.workspace.onDidOpenTextDocument((doc) => diagnosticsProvider.validate(doc)),
    vscode.workspace.onDidChangeTextDocument((e) => diagnosticsProvider.validate(e.document)),
  );

  context.subscriptions.push(
    vscode.workspace.onDidCloseTextDocument((doc) => {
      cache.invalidate(doc);
      diagnosticsProvider.clear(doc);
    }),
  );

  context.subscriptions.push(
    vscode.workspace.onDidChangeConfiguration((e) => {
      if (e.affectsConfiguration("minimalhtml.diagnostics.enabled")) {
        const enabled = vscode.workspace
          .getConfiguration("minimalhtml")
          .get<boolean>("diagnostics.enabled", true);
        if (enabled) {
          diagnosticsProvider.validateAll(vscode.workspace.textDocuments);
        } else {
          diagnosticsProvider.clearAll();
        }
      }
    }),
  );

  context.subscriptions.push(
    vscode.languages.registerDocumentSemanticTokensProvider(
      { language: "csharp" },
      new CsharpHtmlSemanticTokensProvider(cache),
      legend,
    ),
  );
  context.subscriptions.push(
    vscode.languages.registerHoverProvider(
      { language: "csharp" },
      new HtmlHoverProvider(cache),
    ),
  );
  context.subscriptions.push(
    vscode.languages.registerCompletionItemProvider(
      { language: "csharp" },
      new HtmlCompletionProvider(cache),
      "<", " ", "=", '"', "'", "/",
    ),
  );
}

export function deactivate(): void {
  // no-op
}
