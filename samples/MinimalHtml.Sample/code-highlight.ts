// Scan loop adapted from microlighter (MIT License, Copyright (c) Dave Rupert)
// https://github.com/davatron5000/microlighter
//
// The scan loop is a TypeScript transcription of microlighter's highlight.js
// with a custom grammar loader that plugs in the HTML# injection grammar.

import { createGrammarLoader } from "microlighter/grammar-dependencies.js";
import { htmlsharpGrammar, patchedHtmlGrammar, csharpGrammar } from "./grammars/htmlsharp";
import "microlighter/micro-lighter-element.min.js";

type AnyGrammar = Record<string, any>;

const grammarMap: Record<string, AnyGrammar> = {
  htmlsharp: htmlsharpGrammar,
  csharp: csharpGrammar,
  html: patchedHtmlGrammar,
};

const importLanguage = async (language: string): Promise<any> =>
  grammarMap[language] ?? null;

const loadGrammars = createGrammarLoader(importLanguage);

const highlights = new Map<string, Highlight>();

function getCategory(scope: string): string | undefined {
  const parts = scope.split(".");
  const [first, second, third] = parts;
  const last = parts.at(-1);

  if (first === "markup" && ["quote", "inserted", "deleted", "raw"].includes(second!)) return second;
  if (first === "entity" && second === "name") return third;
  if (scope.startsWith("constant.character.entity")) return "character-entity";
  if (parts.includes("numeric")) return "numeric";
  if (scope.startsWith("support.type.property-name")) return "property";
  if (parts.includes("attribute-value")) return "attribute-value";
  if (scope.startsWith("string.other.link")) return "link";

  if ([
    "doctype", "at-rule", "important", "regexp", "boolean",
    "symbol", "operator", "attribute-name",
  ].includes(last!)) return last;

  if ([
    "comment", "string", "constant", "storage", "keyword",
    "variable", "punctuation", "entity", "support",
  ].includes(first!)) return first;
  return undefined;
}

function addRange(node: Text, start: number, end: number, scope: string): void {
  const category = getCategory(scope);
  if (!category || start === end) return;

  const range = new Range();
  range.setStart(node, start);
  range.setEnd(node, end);

  if (!highlights.has(category)) highlights.set(category, new Highlight());
  highlights.get(category)!.add(range);
}

function addCaptures(node: Text, match: any, captures: Record<string, { name: string }> = {}): void {
  for (const [index, capture] of Object.entries(captures)) {
    const offsets = match.indices[index];
    if (offsets) addRange(node, offsets[0], offsets[1], capture.name);
  }
}

function escapeRegex(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

function expandEnd(pattern: string, beginMatch: any): string {
  return pattern.replace(/\\(\d+)/g, (_reference, index) => {
    return beginMatch[index] === undefined ? _reference : escapeRegex(beginMatch[index]);
  });
}

function getRules(rule: any): any[] {
  if (!rule) return [];
  if (Array.isArray(rule)) return rule;
  if (rule.match || rule.begin || rule.include) return [rule];
  return rule.patterns || [];
}

export async function highlightHtmlSharp(options?: {
  root?: ParentNode;
  selector?: string;
}): Promise<HTMLElement[]> {
  const root = options?.root ?? document;
  const selector = options?.selector ?? "pre > code.language-htmlsharp";

  const codeBlocks = [...root.querySelectorAll<HTMLElement>(selector)];
  if (codeBlocks.length === 0) return [];

  const grammars = await loadGrammars(["htmlsharp"]);
  const grammar = grammars.languages["htmlsharp"];
  if (!grammar) return [];

  const regexes = new Map<string, RegExp>();

  highlights.forEach((ranges, category) => {
    if (CSS.highlights.get(category) === ranges) CSS.highlights.delete(category);
    ranges.clear();
  });

  function exec(pattern: string, node: Text, start: number, end: number): any {
    if (!regexes.has(pattern)) regexes.set(pattern, new RegExp(pattern, "dgm"));
    const regex = regexes.get(pattern)!;
    regex.lastIndex = start;
    const match = regex.exec(node.data) as any;
    return match && match.indices[0][0] < end && match.indices[0][1] <= end ? match : null;
  }

  function resolveInclude(include: string, g: any, baseGrammar: any): { grammar: any; rules: any[] } | null {
    if (include === "$self") return { grammar: g, rules: g.patterns };
    if (include === "$base") return { grammar: baseGrammar, rules: baseGrammar.patterns };

    if (include[0] === "#") {
      return { grammar: g, rules: getRules(g.repository?.[include.slice(1)]) };
    }

    const [scopeName, repositoryName] = include.split("#");
    const includedGrammar = grammars.scopes.get(scopeName);
    if (!includedGrammar) return null;

    return {
      grammar: includedGrammar,
      rules: repositoryName
        ? getRules(includedGrammar.repository?.[repositoryName])
        : includedGrammar.patterns,
    };
  }

  function expandRules(
    rules: any[],
    g: any,
    baseGrammar: any,
    activeIncludes: Set<string> = new Set(),
  ): { grammar: any; rule: any }[] {
    const expanded: { grammar: any; rule: any }[] = [];

    for (const rule of rules) {
      if (rule.include) {
        const includeKey = `${g.scopeName}:${rule.include}`;
        if (activeIncludes.has(includeKey)) continue;

        const included = resolveInclude(rule.include, g, baseGrammar);
        if (!included) continue;

        const nestedIncludes = new Set(activeIncludes);
        nestedIncludes.add(includeKey);
        expanded.push(...expandRules(included.rules, included.grammar, baseGrammar, nestedIncludes));
        continue;
      }

      if (rule.match || (rule.begin && rule.end)) {
        expanded.push({ grammar: g, rule });
      }
    }

    return expanded;
  }

  function nextRule(node: Text, contexts: { grammar: any; rule: any }[], start: number, end: number): { grammar: any; rule: any; match: any } | null {
    let winner: { grammar: any; rule: any; match: any } | null = null;

    for (const context of contexts) {
      const pattern = context.rule.match || context.rule.begin;
      const match = exec(pattern, node, start, end);
      if (!match) continue;

      if (!winner || match.indices[0][0] < winner.match.indices[0][0]) {
        winner = { ...context, match };
      }
    }

    return winner;
  }

  function scanRegion(
    node: Text,
    rules: any[],
    start: number,
    end: number,
    g: any,
    baseGrammar: any = g,
    closing: { pattern: string; applyEndPatternLast?: boolean } | null = null,
  ): { contentEnd: number; end: number; match: any } {
    const contexts = expandRules(rules, g, baseGrammar);
    let cursor = start;

    while (cursor < end) {
      const candidate = nextRule(node, contexts, cursor, end);
      const endMatch = closing ? exec(closing.pattern, node, cursor, end) : null;
      const candidateStart = candidate?.match.indices[0][0] ?? Infinity;
      const endStart = endMatch?.indices[0][0] ?? Infinity;

      if (endMatch && closing && (endStart < candidateStart || (endStart === candidateStart && !closing.applyEndPatternLast))) {
        return { contentEnd: endStart, end: endMatch.indices[0][1], match: endMatch };
      }

      if (!candidate) return { contentEnd: end, end, match: null };

      const { rule, match, grammar: ruleGrammar } = candidate;
      if (rule.match) {
        if (rule.name) addRange(node, match.indices[0][0], match.indices[0][1], rule.name);
        addCaptures(node, match, rule.captures);
        cursor = match.indices[0][1] > cursor ? match.indices[0][1] : cursor + 1;
        continue;
      }

      const nested = scanRegion(
        node,
        rule.patterns || [],
        match.indices[0][1],
        end,
        ruleGrammar,
        baseGrammar,
        { pattern: expandEnd(rule.end, match), applyEndPatternLast: rule.applyEndPatternLast },
      );

      if (rule.name) addRange(node, match.indices[0][0], nested.end, rule.name);
      if (rule.contentName) addRange(node, match.indices[0][1], nested.contentEnd, rule.contentName);
      addCaptures(node, match, rule.beginCaptures || rule.captures);
      if (nested.match) addCaptures(node, nested.match, rule.endCaptures || rule.captures);

      cursor = nested.end > cursor ? nested.end : cursor + 1;
    }

    return { contentEnd: end, end, match: null };
  }

  for (const codeBlock of codeBlocks) {
    codeBlock.normalize();
    const node = codeBlock.firstChild;
    if (!node || node.nodeType !== Node.TEXT_NODE || node.nextSibling) continue;
    scanRegion(node as Text, grammar.patterns, 0, (node as Text).data.length, grammar);
  }

  highlights.forEach((ranges, category) => {
    if (ranges.size > 0) CSS.highlights.set(category, ranges);
  });

  return codeBlocks;
}

function init(): void {
  if (typeof CSS === "undefined" || !CSS.highlights) {
    console.warn("[code-highlight] CSS Custom Highlight API not supported in this browser.");
    return;
  }
  document.body.dataset.syntaxTheme = "github";
  void highlightHtmlSharp();
}

if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", init);
} else {
  init();
}

document.addEventListener("syntax-highlight", () => void highlightHtmlSharp());
