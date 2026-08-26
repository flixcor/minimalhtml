import mainGrammar from "../../../extensions/html-sharp/syntaxes/html-in-csharp.injection.tmLanguage.json";
import holesGrammar from "../../../extensions/html-sharp/syntaxes/html-interp-holes.injection.tmLanguage.json";
import htmlBase from "microlighter/grammars/html.js";
import csharpBase from "microlighter/grammars/csharp.js";

type Grammar = Record<string, any>;

const holeRules: Grammar = {
  "hole-n1": holesGrammar.repository["hole-n1"],
  "hole-n2": holesGrammar.repository["hole-n2"],
  "hole-n3": holesGrammar.repository["hole-n3"],
};

const holeIncludes = [
  { include: "#hole-n3" },
  { include: "#hole-n2" },
  { include: "#hole-n1" },
];

function withHoleDetection(grammar: Grammar): Grammar {
  const repo = grammar.repository;
  const patch = (rule: Grammar) => ({
    ...rule,
    patterns: [...holeIncludes, ...(rule.patterns ?? [])],
  });
  return {
    ...grammar,
    repository: {
      ...repo,
      ...holeRules,
      attributes: patch(repo.attributes),
      tags: patch(repo.tags),
      "inline-css": patch(repo["inline-css"]),
      "inline-javascript": patch(repo["inline-javascript"]),
      "unquoted-attributes": patch(repo["unquoted-attributes"]),
    },
  };
}

export const htmlsharpGrammar: Grammar = {
  ...mainGrammar,
  scopeName: "htmlsharp",
  dependencies: ["csharp", "html"],
  patterns: [
    ...mainGrammar.patterns,
    { include: "source.cs" },
  ],
};

export const patchedHtmlGrammar: Grammar = withHoleDetection(htmlBase as Grammar);

export const csharpGrammar: Grammar = csharpBase as Grammar;
