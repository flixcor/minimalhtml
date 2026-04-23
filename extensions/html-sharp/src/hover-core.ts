// Pure-Node hover logic — no vscode import so unit tests run without an extension host.
import { getLanguageService, TextDocument } from "vscode-html-languageservice";
import { parseHtmlRegions, HtmlRegion } from "./marker-parser";

const ls = getLanguageService();

export interface HoverResult {
  contents: string;
}

// truncateAt: virtual-doc offset to stop at (completion path); omit for full body (hover path).
export function buildVirtualHtmlDoc(
  region: HtmlRegion,
  csText: string,
  truncateAt?: number,
): TextDocument {
  const sliceEnd =
    truncateAt !== undefined
      ? region.bodyStart + truncateAt
      : region.bodyEnd;
  const chars = Array.from(csText.slice(region.bodyStart, sliceEnd));

  // Unescape C# string escapes in-place, keeping array length identical so
  // virtual-doc offsets stay 1:1 with C# offsets.
  // Regular/interp-regular: `\"` → ` "` (backslash → space, exposing the real quote)
  // Verbatim/interp-verbatim: `""` → `" ` (first quote kept, second collapsed to space)
  // Raw forms need no unescaping.
  if (region.form === "regular" || region.form === "interp-regular") {
    for (let i = 0; i < chars.length - 1; i++) {
      if (chars[i] === "\\" && chars[i + 1] === '"') {
        chars[i] = " ";
        chars[i + 1] = '"';
        i++;
      }
    }
  } else if (region.form === "verbatim" || region.form === "interp-verbatim") {
    for (let i = 0; i < chars.length - 1; i++) {
      if (chars[i] === '"' && chars[i + 1] === '"') {
        chars[i + 1] = " ";
        i++;
      }
    }
  }

  for (const hole of region.holes) {
    const hStart = hole.start - region.bodyStart;
    const hEnd = Math.min(hole.end - region.bodyStart, chars.length);
    if (hStart >= chars.length) break;
    for (let k = hStart; k < hEnd; k++) chars[k] = " ";
  }
  return TextDocument.create(
    "html-in-cs://0/0.html",
    "html",
    1,
    chars.join(""),
  );
}

export function getHoverForOffset(
  text: string,
  offset: number,
  regions?: HtmlRegion[],
): HoverResult | undefined {
  const resolvedRegions = regions ?? parseHtmlRegions(text);
  const region = resolvedRegions.find(
    (r) => r.bodyStart <= offset && offset < r.bodyEnd,
  );
  if (!region) return undefined;

  const inHole = region.holes.some(
    (h) => h.start <= offset && offset < h.end,
  );
  if (inHole) return undefined;

  const virtualDoc = buildVirtualHtmlDoc(region, text);
  const virtualOffset = offset - region.bodyStart;
  const virtualPos = virtualDoc.positionAt(virtualOffset);

  const htmlDoc = ls.parseHTMLDocument(virtualDoc);
  const hover = ls.doHover(virtualDoc, virtualPos, htmlDoc);
  if (!hover) return undefined;

  const raw = hover.contents;
  let contents: string;
  if (typeof raw === "string") {
    contents = raw;
  } else if (Array.isArray(raw)) {
    contents = raw.map((c) => (typeof c === "string" ? c : c.value)).join("\n");
  } else {
    contents = (raw as { value: string }).value;
  }

  if (!contents) return undefined;
  return { contents };
}
