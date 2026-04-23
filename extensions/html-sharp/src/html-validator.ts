// Pure-Node HTML structural validator — no vscode import; unit tests run without extension host.
import { getLanguageService } from "vscode-html-languageservice";
import { buildVirtualHtmlDoc } from "./hover-core";
import { HtmlRegion } from "./marker-parser";

const ls = getLanguageService();

export interface HtmlDiagnostic {
  startOffset: number;
  endOffset: number;
  message: string;
}

const VOID_ELEMENTS = new Set([
  "area", "base", "br", "col", "embed", "hr", "img", "input",
  "link", "meta", "param", "source", "track", "wbr",
]);

export function validateHtmlRegions(regions: HtmlRegion[], csText: string): HtmlDiagnostic[] {
  const result: HtmlDiagnostic[] = [];

  for (const region of regions) {
    const virtualDoc = buildVirtualHtmlDoc(region, csText);
    const htmlDoc = ls.parseHTMLDocument(virtualDoc);

    function walkNode(node: { tag?: string; start: number; startTagEnd?: number; end: number; endTagStart?: number; children: typeof htmlDoc.roots }): void {
      if (
        node.tag &&
        !VOID_ELEMENTS.has(node.tag.toLowerCase()) &&
        node.endTagStart === undefined
      ) {
        result.push({
          startOffset: region.bodyStart + node.start,
          endOffset: region.bodyStart + (node.startTagEnd ?? node.start + 1),
          message: `Unclosed tag <${node.tag}>`,
        });
      }
      for (const child of node.children) {
        walkNode(child);
      }
    }

    for (const root of htmlDoc.roots) {
      walkNode(root);
    }
  }

  return result;
}
