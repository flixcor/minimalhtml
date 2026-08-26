declare module "microlighter/grammar-dependencies.js" {
  export function normalizeLanguage(language: string, aliases?: Record<string, string>): string;
  export function getExternalLanguages(value: any): Set<string>;
  export function createGrammarLoader(
    importLanguage?: (language: string) => Promise<any>,
  ): (languages: string[]) => Promise<{ languages: Record<string, any>; scopes: Map<string, any> }>;
}

declare module "microlighter/grammars/*.js" {
  const grammar: Record<string, any>;
  export default grammar;
}

declare module "microlighter/themes/*.css";
