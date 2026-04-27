import type { Plugin } from "vite";
import { glob } from "tinyglobby";
import { readFile, stat } from "node:fs/promises";

export interface MinimalHtmlOptions {
  /** Globs to scan for marker comments. Default: ['./**\/*.cs']. */
  scan?: string[];
  /** Marker token inside `/*MARKER*\/"path"`. Default: 'vite'. */
  marker?: string;
  /** Globs to ignore during scan. Default: bin, obj, node_modules. */
  ignore?: string[];
  /** Extra entry inputs to merge with discovered ones. */
  inputs?: string[];
  /** Override entryFileNames. Default: 'assets/[name]-[hash].js'. */
  entryFileNames?: string | ((info: { name: string }) => string);
  /** Disable rolldown experimental chunkImportMap. Default: false. */
  disableImportMap?: boolean;
  /** baseUrl for chunkImportMap. Default: '/'. */
  importMapBaseUrl?: string;
  /** Output filename for chunkImportMap. Default: '.vite/importmap.json'. */
  importMapFileName?: string;
}

const DEFAULT_SCAN = ["./**/*.cs"];
const DEFAULT_MARKER = "vite";
const DEFAULT_IGNORE = ["**/bin/**", "**/obj/**", "**/node_modules/**"];

export default function minimalHtml(options: MinimalHtmlOptions = {}): Plugin {
  const scan = options.scan ?? DEFAULT_SCAN;
  const marker = options.marker ?? DEFAULT_MARKER;
  const ignore = options.ignore ?? DEFAULT_IGNORE;
  const explicit = options.inputs ?? [];
  const entryFileNames =
    options.entryFileNames ?? "assets/[name]-[hash].js";
  const pattern = new RegExp(
    `\\/\\*\\s*${escapeRegex(marker)}\\s*\\*\\/\\s*"([^"]+)"`,
    "g",
  );

  return {
    name: "minimal-html-vite",
    async config() {
      const inputs = await discoverInputs({
        scan,
        ignore,
        pattern,
        explicit,
      });

      const experimental = options.disableImportMap
        ? undefined
        : {
            chunkImportMap: {
              baseUrl: options.importMapBaseUrl ?? "/",
              fileName: options.importMapFileName ?? ".vite/importmap.json",
            },
          };

      return {
        build: {
          manifest: true,
          modulePreload: false,
          rolldownOptions: {
            experimental,
            input: inputs,
            output: {
              format: "esm" as const,
              entryFileNames,
            },
          },
        },
      };
    },
  };
}

interface DiscoverArgs {
  scan: string[];
  ignore: string[];
  pattern: RegExp;
  explicit: string[];
}

async function discoverInputs({
  scan,
  ignore,
  pattern,
  explicit,
}: DiscoverArgs): Promise<string[]> {
  const files = await glob(scan, { ignore });
  const matches = await Promise.all(
    files.map(async (file) => {
      const content = await readFile(file, { encoding: "utf-8" });
      return [...content.matchAll(pattern)].map(([, p]) => p);
    }),
  );
  const distinct = [...new Set([...matches.flat(), ...explicit])];
  const exists = await Promise.all(
    distinct.map((p) =>
      stat(p)
        .then(() => p)
        .catch(() => null),
    ),
  );
  return exists.filter((p): p is string => p !== null);
}

function escapeRegex(s: string): string {
  return s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

export { minimalHtml };
