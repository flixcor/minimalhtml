export type HtmlRegionForm =
  | "regular"
  | "verbatim"
  | "raw3"
  | "interp-regular"
  | "interp-verbatim"
  | "interp-raw-n1"
  | "interp-raw-n2"
  | "interp-raw-n3";

export interface InterpolationHole {
  start: number;
  end: number;
  // Brace-run width N. The hole opener is [start, start+n); the hole closer is
  // [end-n, end); the inner hole body (C# expression) is [start+n, end-n).
  n: number;
}

export interface HtmlRegion {
  form: HtmlRegionForm;
  bodyStart: number;
  bodyEnd: number;
  holes: InterpolationHole[];
}

// See specs/002-interpolated-strings/contracts/grammar.md §2.2 and
// specs/001-html-string-tokenization/contracts/grammar.md §2.2 — this regex
// must stay in sync with the MARKER_REGEX used by the TextMate grammar.
const BLOCK_MARKER = /^\/\*.*?\blang(?:uage)?\s*=\s*html\b.*?\*\/$/is;
const LINE_MARKER = /^\/\/.*?\blang(?:uage)?\s*=\s*html\b/i;

export function parseHtmlRegions(text: string): HtmlRegion[] {
  const regions: HtmlRegion[] = [];

  // A single linear walk over the source, tracking C# lexical state: strings
  // (regular, verbatim, raw3, plus their `$`-interpolated variants) are
  // skipped as opaque spans, comments are inspected for markers, and a
  // pending-marker flag promotes the next adjacent string literal.
  let i = 0;
  let pendingMarker = false;

  while (i < text.length) {
    const ch = text[i];

    if (ch === "$") {
      // Count the $-run length; dispatch by (n, next-char).
      let n = 0;
      while (text[i + n] === "$") n++;
      const after = i + n;

      // Regular interpolated: `$"..."` (n=1, no `@`, not triple-quote).
      if (n === 1 && text[after] === '"' && text[after + 1] !== '"') {
        const bodyStart = after + 1;
        const result = findInterpRegularEnd(text, bodyStart);
        if (!result) {
          pendingMarker = false;
          i = skipInterpolatedString(text, i);
          continue;
        }
        if (pendingMarker) {
          regions.push({
            form: "interp-regular",
            bodyStart,
            bodyEnd: result.bodyEnd,
            holes: result.holes,
          });
          pendingMarker = false;
        }
        i = result.bodyEnd + 1;
        continue;
      }

      // Verbatim interpolated: `$@"..."` (n=1, followed by `@"`).
      if (n === 1 && text[after] === "@" && text[after + 1] === '"') {
        const bodyStart = after + 2;
        const result = findInterpVerbatimEnd(text, bodyStart);
        if (!result) {
          pendingMarker = false;
          i = skipInterpolatedString(text, i);
          continue;
        }
        if (pendingMarker) {
          regions.push({
            form: "interp-verbatim",
            bodyStart,
            bodyEnd: result.bodyEnd,
            holes: result.holes,
          });
          pendingMarker = false;
        }
        i = result.bodyEnd + 1;
        continue;
      }

      // Raw interpolated: `$"""`, `$$"""`, `$$$"""` (n∈{1,2,3}, followed by
      // exactly 3 quotes). N>=4 stays inert per FR-011. N>=4 opening quotes
      // are also unsupported (R3 carry-over); we detect and skip.
      if (
        n >= 1 && n <= 3 &&
        text[after] === '"' && text[after + 1] === '"' && text[after + 2] === '"' &&
        text[after + 3] !== '"'
      ) {
        const bodyStart = after + 3;
        const result = findInterpRawNEnd(text, bodyStart, n);
        if (!result) {
          pendingMarker = false;
          i = skipInterpolatedString(text, i);
          continue;
        }
        if (pendingMarker) {
          const form = n === 1 ? "interp-raw-n1" : n === 2 ? "interp-raw-n2" : "interp-raw-n3";
          regions.push({
            form,
            bodyStart,
            bodyEnd: result.bodyEnd,
            holes: result.holes,
          });
          pendingMarker = false;
        }
        i = result.bodyEnd + 3;
        continue;
      }

      pendingMarker = false;
      i = skipInterpolatedString(text, i);
      continue;
    }

    if (ch === "@" && text[i + 1] === "$" && text[i + 2] === '"') {
      // Verbatim interpolated with swapped prefix: `@$"..."`.
      const bodyStart = i + 3;
      const result = findInterpVerbatimEnd(text, bodyStart);
      if (!result) {
        pendingMarker = false;
        i = skipInterpolatedString(text, i + 1);
        continue;
      }
      if (pendingMarker) {
        regions.push({
          form: "interp-verbatim",
          bodyStart,
          bodyEnd: result.bodyEnd,
          holes: result.holes,
        });
        pendingMarker = false;
      }
      i = result.bodyEnd + 1;
      continue;
    }

    if (ch === "@" && text[i + 1] === '"') {
      const bodyStart = i + 2;
      const bodyEnd = findVerbatimEnd(text, bodyStart);
      if (bodyEnd === -1) return regions;
      if (pendingMarker) {
        regions.push({ form: "verbatim", bodyStart, bodyEnd, holes: [] });
        pendingMarker = false;
      }
      i = bodyEnd + 1;
      continue;
    }

    if (ch === '"' && text[i + 1] === '"' && text[i + 2] === '"') {
      // 4+ consecutive quotes → R3: raw literals with N>3 opening quotes are
      // unsupported; treat as unpromotable and skip to the corresponding end.
      if (text[i + 3] === '"') {
        pendingMarker = false;
        i = skipRawNQuoteString(text, i);
        continue;
      }
      const bodyStart = i + 3;
      const bodyEnd = findRaw3End(text, bodyStart);
      if (bodyEnd === -1) return regions;
      if (pendingMarker) {
        regions.push({ form: "raw3", bodyStart, bodyEnd, holes: [] });
        pendingMarker = false;
      }
      i = bodyEnd + 3;
      continue;
    }

    if (ch === '"') {
      const bodyStart = i + 1;
      const bodyEnd = findRegularLiteralEnd(text, bodyStart);
      if (bodyEnd === -1) {
        // Unterminated on this line — skip the opener and keep scanning.
        i++;
        continue;
      }
      if (pendingMarker) {
        regions.push({ form: "regular", bodyStart, bodyEnd, holes: [] });
        pendingMarker = false;
      }
      i = bodyEnd + 1;
      continue;
    }

    if (ch === "/" && text[i + 1] === "*") {
      const commentEnd = text.indexOf("*/", i + 2);
      if (commentEnd === -1) return regions;
      const commentText = text.slice(i, commentEnd + 2);
      if (BLOCK_MARKER.test(commentText)) {
        pendingMarker = true;
      }
      i = commentEnd + 2;
      continue;
    }

    if (ch === "/" && text[i + 1] === "/") {
      const lineEnd = text.indexOf("\n", i + 2);
      const end = lineEnd === -1 ? text.length : lineEnd;
      const commentText = text.slice(i, end);
      if (LINE_MARKER.test(commentText)) {
        pendingMarker = true;
      }
      i = end;
      continue;
    }

    // Html( method trigger: scan identifier starting at `H`, check for
    // exactly "Html" with word boundary, then optional whitespace, then `(`.
    if (ch === "H") {
      let j = i;
      while (j < text.length && isIdentChar(text.charCodeAt(j))) j++;
      const ident = text.slice(i, j);
      const prevCode = i > 0 ? text.charCodeAt(i - 1) : 0;
      if (ident === "Html" && !isIdentChar(prevCode)) {
        let k = j;
        while (k < text.length && isWhitespace(text.charCodeAt(k))) k++;
        if (text[k] === "(") {
          pendingMarker = true;
          i = k + 1;
          continue;
        }
      }
      // Non-Html identifier (or Html without paren): clear pendingMarker and skip.
      if (pendingMarker && !isWhitespace(text.charCodeAt(i))) pendingMarker = false;
      i = j > i ? j : i + 1;
      continue;
    }

    if (pendingMarker && !isWhitespace(text.charCodeAt(i))) {
      pendingMarker = false;
    }
    i++;
  }

  return regions;
}

// Returns the ordered list of half-open text-span ranges for a region —
// the body minus every hole's INNER body (the C# expression between the
// N-brace opener and N-brace closer). The opener and closer brace runs are
// INCLUDED in the surrounding text span so the semantic-tokens provider
// paints them too; this masks Roslyn's inconsistent semantic coloring of
// `{{{`/`}}}` at N>1, and TextMate's `punctuation.section.interpolation.*`
// scope then supplies the brace coloring via theme fall-through.
export function textSpansOf(region: HtmlRegion): Array<{ start: number; end: number }> {
  if (region.holes.length === 0) {
    if (region.bodyEnd <= region.bodyStart) return [];
    return [{ start: region.bodyStart, end: region.bodyEnd }];
  }
  const spans: Array<{ start: number; end: number }> = [];
  let cursor = region.bodyStart;
  for (const hole of region.holes) {
    const innerStart = hole.start + hole.n;
    const innerEnd = hole.end - hole.n;
    if (innerStart > cursor) {
      spans.push({ start: cursor, end: innerStart });
    }
    cursor = innerEnd;
  }
  if (cursor < region.bodyEnd) {
    spans.push({ start: cursor, end: region.bodyEnd });
  }
  return spans;
}

function isWhitespace(code: number): boolean {
  return code === 0x20 || code === 0x09 || code === 0x0a || code === 0x0d || code === 0x0b || code === 0x0c;
}

function isIdentChar(code: number): boolean {
  return (code >= 48 && code <= 57) ||  // 0-9
    (code >= 65 && code <= 90) ||       // A-Z
    code === 95 ||                       // _
    (code >= 97 && code <= 122);        // a-z
}

// Walks the body of a `$"..."` literal starting at bodyStart. Identifies
// holes (exactly-1-brace rule per FR-004) and returns the bodyEnd offset
// (position of the closing `"`). Returns null if the literal is unterminated.
function findInterpRegularEnd(
  text: string,
  bodyStart: number,
): { bodyEnd: number; holes: InterpolationHole[] } | null {
  const holes: InterpolationHole[] = [];
  let i = bodyStart;
  while (i < text.length) {
    const ch = text[i];
    if (ch === "\\") {
      i += 2;
      continue;
    }
    if (ch === "\n") return null;
    if (ch === '"') return { bodyEnd: i, holes };
    if (ch === "{") {
      // Exactly-1 rule: a `{` adjacent to another `{` is literal (escape pair),
      // not a hole opener. Both chars of a `{{` run fail this check.
      if (text[i + 1] === "{") {
        i += 2;
        continue;
      }
      const holeStart = i;
      const holeEnd = walkHoleBody(text, i + 1);
      if (holeEnd === -1) return null;
      holes.push({ start: holeStart, end: holeEnd + 1, n: 1 });
      i = holeEnd + 1;
      continue;
    }
    i++;
  }
  return null;
}

// Walks the body of a `$@"..."` / `@$"..."` literal starting at bodyStart.
// Verbatim rules: `""` is a literal-quote escape (stays inside); single `"`
// terminates. Newlines are allowed. Hole opener is exactly 1 `{`.
function findInterpVerbatimEnd(
  text: string,
  bodyStart: number,
): { bodyEnd: number; holes: InterpolationHole[] } | null {
  const holes: InterpolationHole[] = [];
  let i = bodyStart;
  while (i < text.length) {
    const ch = text[i];
    if (ch === '"') {
      if (text[i + 1] === '"') {
        i += 2;
        continue;
      }
      return { bodyEnd: i, holes };
    }
    if (ch === "{") {
      if (text[i + 1] === "{") {
        i += 2;
        continue;
      }
      const holeStart = i;
      const holeEnd = walkHoleBodyN(text, i + 1, 1);
      if (holeEnd === -1) return null;
      holes.push({ start: holeStart, end: holeEnd + 1, n: 1 });
      i = holeEnd + 1;
      continue;
    }
    i++;
  }
  return null;
}

// Walks the body of a raw interpolated literal (`$"""`, `$$"""`, `$$$"""`)
// with N leading `$`. Rules: `"""` terminates (a run of 1 or 2 `"` is literal
// text, only three-in-a-row close). No `\` escapes. Hole opener is EXACTLY N
// consecutive `{`; hole closer is EXACTLY N consecutive `}` at depth 0.
// Shorter runs are literal text. Newlines are allowed.
function findInterpRawNEnd(
  text: string,
  bodyStart: number,
  n: number,
): { bodyEnd: number; holes: InterpolationHole[] } | null {
  const holes: InterpolationHole[] = [];
  let i = bodyStart;
  while (i < text.length) {
    const ch = text[i];
    if (ch === '"') {
      let q = 0;
      while (text[i + q] === '"') q++;
      if (q >= 3) {
        return { bodyEnd: i, holes };
      }
      i += q;
      continue;
    }
    if (ch === "{") {
      let run = 0;
      while (text[i + run] === "{") run++;
      if (run < n) {
        i += run;
        continue;
      }
      // Longer runs: take leftmost N as opener, remainder becomes the first
      // characters of the hole body (walkHoleBodyN handles nested `{`/`}`).
      const openerEnd = i + n;
      const holeStart = i;
      const holeEnd = walkHoleBodyN(text, openerEnd, n);
      if (holeEnd === -1) return null;
      holes.push({ start: holeStart, end: holeEnd + n, n });
      i = holeEnd + n;
      continue;
    }
    i++;
  }
  return null;
}

// Walks a hole body starting one past the opening `{`. Returns the offset of
// the matching `}` at depth 0. Tracks nested `{`/`}` balance and skips nested
// string literals using the same helpers the top-level walker uses, so an
// inner `"` inside the hole cannot terminate the outer literal.
function walkHoleBody(text: string, i: number): number {
  return walkHoleBodyN(text, i, 1);
}

// N-aware hole-body walker: closer is EXACTLY N consecutive `}` at depth 0.
// Shorter runs are literal text (don't close). Longer runs take the leftmost
// N as the closer. Used for both regular/verbatim (N=1) and raw-nN forms.
function walkHoleBodyN(text: string, i: number, n: number): number {
  let depth = 0;
  while (i < text.length) {
    const ch = text[i];
    if (ch === "\n") {
      i++;
      continue;
    }
    if (ch === "$") {
      i = skipInterpolatedString(text, i);
      continue;
    }
    if (ch === "@" && text[i + 1] === '"') {
      const end = findVerbatimEnd(text, i + 2);
      if (end === -1) return -1;
      i = end + 1;
      continue;
    }
    if (ch === '"' && text[i + 1] === '"' && text[i + 2] === '"') {
      if (text[i + 3] === '"') {
        i = skipRawNQuoteString(text, i);
        continue;
      }
      const end = findRaw3End(text, i + 3);
      if (end === -1) return -1;
      i = end + 3;
      continue;
    }
    if (ch === '"') {
      const end = findRegularLiteralEnd(text, i + 1);
      if (end === -1) return -1;
      i = end + 1;
      continue;
    }
    if (ch === "{") {
      let run = 0;
      while (text[i + run] === "{") run++;
      depth += run;
      i += run;
      continue;
    }
    if (ch === "}") {
      let run = 0;
      while (text[i + run] === "}") run++;
      if (depth === 0 && run >= n) {
        // Position of the FIRST `}` in the closer run. Caller adds n to
        // compute the half-open end offset.
        return i;
      }
      depth -= run;
      if (depth < 0) depth = 0;
      i += run;
      continue;
    }
    i++;
  }
  return -1;
}

function findRegularLiteralEnd(text: string, bodyStart: number): number {
  for (let i = bodyStart; i < text.length; i++) {
    const ch = text[i];
    if (ch === "\\") {
      i++;
      continue;
    }
    if (ch === '"') return i;
    if (ch === "\n") return -1;
  }
  return -1;
}

function findVerbatimEnd(text: string, bodyStart: number): number {
  for (let i = bodyStart; i < text.length; i++) {
    if (text[i] === '"') {
      if (text[i + 1] === '"') {
        i++;
        continue;
      }
      return i;
    }
  }
  return -1;
}

function findRaw3End(text: string, bodyStart: number): number {
  for (let i = bodyStart; i < text.length; i++) {
    if (text[i] === '"' && text[i + 1] === '"' && text[i + 2] === '"') {
      return i;
    }
  }
  return -1;
}

// Skip past an interpolated literal ($"..." / $@"..." / $"""...""") without
// promoting it. Returns the index immediately after the closing delimiter.
function skipInterpolatedString(text: string, i: number): number {
  let n = 0;
  while (text[i + n] === "$") n++;
  let j = i + n;
  if (text[j] === "@" && text[j + 1] === '"') {
    const end = findVerbatimEnd(text, j + 2);
    return end === -1 ? text.length : end + 1;
  }
  if (text[j] === '"' && text[j + 1] === '"' && text[j + 2] === '"') {
    if (text[j + 3] === '"') {
      return skipRawNQuoteString(text, j);
    }
    const end = findRaw3End(text, j + 3);
    return end === -1 ? text.length : end + 3;
  }
  if (text[j] === '"') {
    const end = findRegularLiteralEnd(text, j + 1);
    return end === -1 ? j + 1 : end + 1;
  }
  return j;
}

// Skip past a raw literal with N≥4 opening quotes. Closer is the same
// run of N quotes. Returns the index immediately after the closer.
function skipRawNQuoteString(text: string, start: number): number {
  let n = 0;
  while (text[start + n] === '"') n++;
  const body = start + n;
  for (let i = body; i <= text.length - n; i++) {
    let match = true;
    for (let k = 0; k < n; k++) {
      if (text[i + k] !== '"') {
        match = false;
        break;
      }
    }
    if (match) return i + n;
  }
  return text.length;
}
