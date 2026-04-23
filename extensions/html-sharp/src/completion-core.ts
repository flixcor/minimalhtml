// Pure-Node completion logic — no vscode import so unit tests run without an extension host.
import { getLanguageService, TextDocument, CompletionItem, InsertTextFormat } from "vscode-html-languageservice";
import { parseHtmlRegions, HtmlRegion } from "./marker-parser";
import { buildVirtualHtmlDoc } from "./hover-core";

const ls = getLanguageService();

export interface CompletionResultItem {
  label: string;
  kind?: number;
  insertText?: string;
  insertTextIsSnippet: boolean;
  textEditRange?: { startOffset: number; endOffset: number };
  documentation?: string;
  detail?: string;
  filterText?: string;
  sortText?: string;
}

export interface CompletionResult {
  items: CompletionResultItem[];
  isIncomplete: boolean;
}

function mapItem(
  item: CompletionItem,
  bodyStart: number,
  virtualDoc: TextDocument,
): CompletionResultItem {
  let textEditRange: { startOffset: number; endOffset: number } | undefined;
  let insertText: string | undefined = item.insertText;
  if (item.textEdit) {
    insertText = item.textEdit.newText;
    // Use virtualDoc.offsetAt() — correctly handles multi-line virtual docs.
    if ("range" in item.textEdit) {
      textEditRange = {
        startOffset: bodyStart + virtualDoc.offsetAt(item.textEdit.range.start),
        endOffset: bodyStart + virtualDoc.offsetAt(item.textEdit.range.end),
      };
    } else {
      textEditRange = {
        startOffset: bodyStart + virtualDoc.offsetAt(item.textEdit.insert.start),
        endOffset: bodyStart + virtualDoc.offsetAt(item.textEdit.insert.end),
      };
    }
  }

  let documentation: string | undefined;
  const doc = item.documentation;
  if (typeof doc === "string") {
    documentation = doc;
  } else if (doc && typeof doc === "object" && "value" in doc) {
    documentation = (doc as { value: string }).value;
  }

  return {
    label: item.label,
    kind: item.kind,
    insertText,
    insertTextIsSnippet: item.insertTextFormat === InsertTextFormat.Snippet,
    textEditRange,
    documentation,
    detail: item.detail,
    filterText: item.filterText,
    sortText: item.sortText,
  };
}

export function getCompletionsForOffset(
  text: string,
  offset: number,
  regions?: HtmlRegion[],
): CompletionResult | undefined {
  try {
    const resolvedRegions = regions ?? parseHtmlRegions(text);
    const region = resolvedRegions.find(
      (r) => r.bodyStart <= offset && offset < r.bodyEnd,
    );
    if (!region) return undefined;

    const inHole = region.holes.some(
      (h) => h.start <= offset && offset < h.end,
    );
    if (inHole) return undefined;

    // Truncate virtual doc at cursor — the LS only needs content up to the cursor
    // to determine completion context. This is essential for multi-line raw strings:
    // trailing content (e.g. closing tags on the next line) confuses the LS parser
    // and causes it to return wrong completions (e.g. element names instead of
    // attribute names when the cursor is inside an open tag).
    const virtualOffset = offset - region.bodyStart;
    const virtualDoc = buildVirtualHtmlDoc(region, text, virtualOffset);
    const virtualPos = virtualDoc.positionAt(virtualOffset);
    const htmlDoc = ls.parseHTMLDocument(virtualDoc);
    const list = ls.doComplete(virtualDoc, virtualPos, htmlDoc);

    if (!list) return { items: [], isIncomplete: false };

    return {
      items: list.items.map((item) => mapItem(item, region.bodyStart, virtualDoc)),
      isIncomplete: list.isIncomplete,
    };
  } catch {
    return undefined;
  }
}
