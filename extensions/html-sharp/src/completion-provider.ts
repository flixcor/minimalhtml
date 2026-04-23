import * as vscode from "vscode";
import { getCompletionsForOffset, CompletionResultItem } from "./completion-core";
import { ParseCache } from "./parse-cache";

export { getCompletionsForOffset, CompletionResult, CompletionResultItem } from "./completion-core";

// LSP CompletionItemKind → vscode.CompletionItemKind
const KIND_MAP: Record<number, vscode.CompletionItemKind> = {
  10: vscode.CompletionItemKind.Property,
  11: vscode.CompletionItemKind.Unit,
  12: vscode.CompletionItemKind.Value,
  14: vscode.CompletionItemKind.Keyword,
  17: vscode.CompletionItemKind.Color,
  21: vscode.CompletionItemKind.Field,
};

function toVscodeItem(
  item: CompletionResultItem,
  document: vscode.TextDocument,
): vscode.CompletionItem {
  const vsItem = new vscode.CompletionItem(item.label);

  if (item.kind !== undefined) {
    vsItem.kind = KIND_MAP[item.kind] ?? vscode.CompletionItemKind.Text;
  }

  if (item.insertText !== undefined) {
    vsItem.insertText = item.insertTextIsSnippet
      ? new vscode.SnippetString(item.insertText)
      : item.insertText;
  }

  if (item.textEditRange) {
    vsItem.range = new vscode.Range(
      document.positionAt(item.textEditRange.startOffset),
      document.positionAt(item.textEditRange.endOffset),
    );
  }

  if (item.documentation) {
    vsItem.documentation = new vscode.MarkdownString(item.documentation);
  }

  if (item.detail) vsItem.detail = item.detail;
  if (item.filterText) vsItem.filterText = item.filterText;
  if (item.sortText) vsItem.sortText = item.sortText;

  return vsItem;
}

export class HtmlCompletionProvider implements vscode.CompletionItemProvider {
  constructor(private cache: ParseCache) {}

  provideCompletionItems(
    document: vscode.TextDocument,
    position: vscode.Position,
    _token: vscode.CancellationToken,
    _context: vscode.CompletionContext,
  ): vscode.ProviderResult<vscode.CompletionItem[]> {
    const regions = this.cache.getRegions(document);
    const offset = document.offsetAt(position);
    const result = getCompletionsForOffset(document.getText(), offset, regions);
    if (!result) return undefined;
    return result.items.map((item) => toVscodeItem(item, document));
  }
}
