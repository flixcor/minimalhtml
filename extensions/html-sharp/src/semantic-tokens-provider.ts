import * as vscode from "vscode";
import { textSpansOf } from "./marker-parser";
import { ParseCache } from "./parse-cache";

// Custom token type with no theme rule. Roslyn emits `string` over the entire
// literal body; if our provider ALSO emits `string`, the theme's `string` rule
// wins and paints flat over the HTML TextMate scopes (the SC-006 regression).
// Emitting a custom type that no theme has a rule for makes VS Code fall
// through to the position's TextMate scope coloring, letting our injection
// grammar's HTML scopes win. The token still displaces Roslyn's `string`
// over the same range because later-registered providers' tokens replace
// overlaps.
export const legend = new vscode.SemanticTokensLegend(["embeddedHtml"], []);

export class CsharpHtmlSemanticTokensProvider
  implements vscode.DocumentSemanticTokensProvider
{
  constructor(private cache: ParseCache) {}

  provideDocumentSemanticTokens(
    document: vscode.TextDocument,
    cancellationToken: vscode.CancellationToken,
  ): vscode.SemanticTokens {
    if (cancellationToken.isCancellationRequested) {
      return new vscode.SemanticTokens(new Uint32Array());
    }

    const builder = new vscode.SemanticTokensBuilder(legend);
    const regions = this.cache.getRegions(document);

    if (cancellationToken.isCancellationRequested) {
      return new vscode.SemanticTokens(new Uint32Array());
    }

    // Emit one token per text span (not per region): interpolated literals
    // have one span per contiguous text segment between holes. Non-interpolated
    // regions collapse to a single span equal to [bodyStart, bodyEnd).
    for (const region of regions) {
      for (const span of textSpansOf(region)) {
        const startPos = document.positionAt(span.start);
        const endPos = document.positionAt(span.end);
        this.addTokenRange(builder, startPos, endPos);
      }
    }

    return builder.build();
  }

  private addTokenRange(
    builder: vscode.SemanticTokensBuilder,
    start: vscode.Position,
    end: vscode.Position,
  ): void {
    if (start.line === end.line) {
      builder.push(start.line, start.character, end.character - start.character, 0, 0);
      return;
    }
    builder.push(start.line, start.character, Number.MAX_SAFE_INTEGER, 0, 0);
    for (let line = start.line + 1; line < end.line; line++) {
      builder.push(line, 0, Number.MAX_SAFE_INTEGER, 0, 0);
    }
    if (end.character > 0) {
      builder.push(end.line, 0, end.character, 0, 0);
    }
  }
}
