import type { Plugin } from "vite";
import { glob } from "tinyglobby";
import { readFile, stat, writeFile } from "node:fs/promises";
import manifestSRI from "vite-plugin-manifest-sri";

type SriAlgorithm = "sha256" | "sha384" | "sha512";

export interface MinimalHtmlOptions {
  /** Globs to scan for marker comments. Default: ['./**\/*.cs']. */
  scan?: string[];
  /** Marker token inside `/*MARKER*\/"path"`. Default: 'vite'. */
  marker?: string;
  /** Globs to ignore during scan. Default: bin, obj, node_modules. */
  ignore?: string[];
  /** Extra entry inputs to merge with discovered ones. */
  inputs?: string[];
  /** Disable rolldown experimental chunkImportMap. Default: false. */
  disableImportMap?: boolean;
  /** baseUrl for chunkImportMap. Default: '/'. */
  importMapBaseUrl?: string;
  /** Output filename for chunkImportMap. Default: '.vite/importmap.json'. */
  importMapFileName?: string;
  /** Disable Subresource Integrity hash emission. Default: false. */
  disableIntegrity?: boolean;
  /** SRI hash algorithms. Default: ['sha384']. */
  integrityAlgorithms?: SriAlgorithm[];
  /** Manifest paths the SRI plugin patches. Default: ['.vite/manifest.json']. */
  integrityManifestPaths?: string[];
  /** Enable CSS Modules support: writes `<file>.module.css.json` next to each `.module.css` so the MinimalHtml.Vite source generator can emit a `Classes` helper. Pass `true` or `{}` to enable. */
  cssModules?: boolean | CssModulesPluginOptions;
}

export interface CssModulesPluginOptions {
  /** Disable CSS modules sidecar even when this object is supplied. Default: true. */
  enabled?: boolean;
}

const DEFAULT_SCAN = ["./**/*.cs"];
const DEFAULT_MARKER = "vite";
const DEFAULT_IGNORE = ["**/bin/**", "**/obj/**", "**/node_modules/**"];
const DEFAULT_SRI_MANIFEST_PATHS = [".vite/manifest.json"];

export default function minimalHtml(
  options: MinimalHtmlOptions = {},
): Plugin[] {
  const scan = options.scan ?? DEFAULT_SCAN;
  const marker = options.marker ?? DEFAULT_MARKER;
  const ignore = options.ignore ?? DEFAULT_IGNORE;
  const explicit = options.inputs ?? [];
  const pattern = new RegExp(
    `\\/\\*\\s*${escapeRegex(marker)}\\s*\\*\\/\\s*"([^"]+)"`,
    "g",
  );

  const cssModulesEnabled =
    options.cssModules === true ||
    (!!options.cssModules &&
      typeof options.cssModules === "object" &&
      options.cssModules.enabled !== false);

  const corePlugin: Plugin = {
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

      const clientBuild = {
        manifest: true,
        modulePreload: false,
        rolldownOptions: {
          experimental,
          input: inputs,
        },
      };

      const cssConfig = cssModulesEnabled
        ? {
            css: {
              modules: {
                getJSON: (cssFileName: string, json: Record<string, string>) =>
                  writeFile(cssFileName + ".json", JSON.stringify(json)),
              },
            },
          }
        : {};

      return { build: clientBuild, ...cssConfig };
    },
  };

  const plugins: Plugin[] = [corePlugin];

  if (!options.disableIntegrity) {
    plugins.push(
      manifestSRI({
        algorithms: options.integrityAlgorithms ?? ["sha384"],
        manifestPaths:
          options.integrityManifestPaths ?? DEFAULT_SRI_MANIFEST_PATHS,
      }),
    );
  }

  return plugins;
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
  const resolved = await Promise.all(
    distinct.map((p) => {
      // Markers like `/Pages/Foo.ts` are project-relative (the C# side trims
      // the leading slash too) — strip it so `stat` doesn't look at fs root.
      const localPath = p.startsWith("/") ? p.slice(1) : p;
      return stat(localPath)
        .then(() => localPath)
        .catch(() => null);
    }),
  );
  return resolved.filter((p): p is string => p !== null);
}

function escapeRegex(s: string): string {
  return s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

export { minimalHtml };
