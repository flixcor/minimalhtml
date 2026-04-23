import * as vscode from "vscode";
import { getHoverForOffset } from "./hover-core";
import { ParseCache } from "./parse-cache";

export { getHoverForOffset, HoverResult } from "./hover-core";

export class HtmlHoverProvider implements vscode.HoverProvider {
  constructor(private cache: ParseCache) {}

  provideHover(
    document: vscode.TextDocument,
    position: vscode.Position,
    _token: vscode.CancellationToken,
  ): vscode.ProviderResult<vscode.Hover> {
    const regions = this.cache.getRegions(document);
    const offset = document.offsetAt(position);
    const result = getHoverForOffset(document.getText(), offset, regions);
    if (!result) return undefined;
    return new vscode.Hover(new vscode.MarkdownString(result.contents));
  }
}
