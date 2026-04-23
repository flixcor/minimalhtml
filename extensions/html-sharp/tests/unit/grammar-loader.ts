// Shared vscode-textmate + vscode-oniguruma harness used by the
// grammar-parser-sync cross-layer test (T016/T041/T056). Keeps grammar
// loading out of the `.test.ts` files so mocha's glob doesn't pick this up.

import { readFile } from "node:fs/promises";
import * as path from "node:path";
import * as oniguruma from "vscode-oniguruma";
import * as vsctm from "vscode-textmate";

const REPO_ROOT = path.resolve(__dirname, "..", "..", "..");

const INJECTION_SCOPE = "html-in-csharp.injection";
const CSHARP_SCOPE = "source.cs";
const HTML_SCOPE = "text.html.basic";

const OUTER_EMBEDDED_SCOPE = "meta.embedded.block.html.cs";

let registryPromise: Promise<vsctm.Registry> | undefined;

function createRegistry(): Promise<vsctm.Registry> {
  const onigWasmPath = require.resolve("vscode-oniguruma/release/onig.wasm");

  const onigLib = readFile(onigWasmPath).then(async (wasmBytes) => {
    await oniguruma.loadWASM(wasmBytes.buffer);
    return {
      createOnigScanner: (patterns: string[]) => new oniguruma.OnigScanner(patterns),
      createOnigString: (s: string) => new oniguruma.OnigString(s),
    };
  });

  return Promise.resolve(
    new vsctm.Registry({
      onigLib,
      async loadGrammar(scopeName: string) {
        const grammarPath = grammarPathForScope(scopeName);
        if (!grammarPath) return null;
        const raw = await readFile(grammarPath, "utf8");
        return vsctm.parseRawGrammar(raw, grammarPath);
      },
      getInjections(scopeName: string) {
        if (scopeName === CSHARP_SCOPE) return [INJECTION_SCOPE];
        return undefined;
      },
    }),
  );
}

function grammarPathForScope(scopeName: string): string | undefined {
  switch (scopeName) {
    case INJECTION_SCOPE:
      return path.join(REPO_ROOT, "syntaxes", "html-in-csharp.injection.tmLanguage.json");
    case CSHARP_SCOPE:
      return path.join(REPO_ROOT, "tests", "tmgrammar", "vendor", "csharp.tmLanguage.json");
    case HTML_SCOPE:
      return path.join(REPO_ROOT, "tests", "tmgrammar", "vendor", "html.tmLanguage.json");
    default:
      return undefined;
  }
}

async function getCSharpGrammar(): Promise<vsctm.IGrammar> {
  if (!registryPromise) registryPromise = createRegistry();
  const registry = await registryPromise;
  const grammar = await registry.loadGrammar(CSHARP_SCOPE);
  if (!grammar) throw new Error(`Failed to load grammar for ${CSHARP_SCOPE}`);
  return grammar;
}

export interface ScopeRange {
  start: number;
  end: number;
}

/**
 * Tokenize a C# fixture through the injection grammar and return the ordered,
 * non-overlapping byte ranges where `meta.embedded.block.html.cs` appears in
 * the scope stack. Contiguous tokens carrying the scope are merged. Byte
 * offsets are computed assuming ASCII fixture content (the v2 fixtures
 * contain no multi-byte characters).
 */
export async function outerEmbeddedScopeRanges(text: string): Promise<ScopeRange[]> {
  const grammar = await getCSharpGrammar();

  const lines = text.split(/(?<=\r\n|\r(?!\n)|\n)/);
  const ranges: ScopeRange[] = [];
  let ruleStack = vsctm.INITIAL;
  let byteOffset = 0;

  for (const line of lines) {
    const result = grammar.tokenizeLine(line, ruleStack);
    for (const token of result.tokens) {
      if (token.scopes.includes(OUTER_EMBEDDED_SCOPE)) {
        const start = byteOffset + token.startIndex;
        const end = byteOffset + Math.min(token.endIndex, line.length);
        if (end <= start) continue;
        const last = ranges[ranges.length - 1];
        if (last && last.end === start) {
          last.end = end;
        } else {
          ranges.push({ start, end });
        }
      }
    }
    byteOffset += line.length;
    ruleStack = result.ruleStack;
  }

  return ranges;
}
