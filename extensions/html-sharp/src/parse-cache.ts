import * as vscode from "vscode";
import { parseHtmlRegions, HtmlRegion } from "./marker-parser";

interface CacheEntry {
  version: number;
  regions: HtmlRegion[];
}

export class ParseCache {
  private _entries = new Map<string, CacheEntry>();

  getRegions(document: vscode.TextDocument): HtmlRegion[] {
    const key = document.uri.toString();
    const entry = this._entries.get(key);
    if (entry && entry.version === document.version) {
      return entry.regions;
    }
    const text = document.getText();
    const regions =
      text.includes("lang") || text.includes("Html")
        ? parseHtmlRegions(text)
        : [];
    this._entries.set(key, { version: document.version, regions });
    return regions;
  }

  invalidate(document: vscode.TextDocument): void {
    this._entries.delete(document.uri.toString());
  }
}
