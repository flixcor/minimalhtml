import type { Plugin } from "vite";
import { glob } from "tinyglobby";
import { readFile, stat, writeFile } from "node:fs/promises";
import path from "node:path";
import manifestSRI from "vite-plugin-manifest-sri";

type SriAlgorithm = "sha256" | "sha384" | "sha512";

export interface LitPluginOptions {
  /** Disable Lit integration even when this object is supplied. Default: true. */
  enabled?: boolean;
  /** Extra modules to inline into the SSR bundle (third-party libraries / SSR-only components). */
  modules?: string[];
  /** Output directory for the SSR bundle, relative to project root. Default: 'dist/server'. */
  ssrOutDir?: string;
  /** SSR entry file name. Default: 'server.js'. */
  serverFileName?: string;
  /** Sidecar file (relative to client outDir) telling the .NET side where to find the SSR bundle. Default: '.vite/ssr.json'. */
  ssrInfoFileName?: string;
  /** Virtual module id for the client hydration shim. Default: 'virtual:minimal-html/lit-hydrate'. */
  hydrateVirtualId?: string;
  /** Virtual module id for the SSR entry. Default: 'virtual:minimal-html/lit-server'. */
  serverVirtualId?: string;
}

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
  /** Enable Lit SSR + hydration support. Pass `{}` to enable with defaults. */
  lit?: LitPluginOptions;
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

const DEFAULT_LIT_HYDRATE_ID = "virtual:minimal-html/lit-hydrate";
const DEFAULT_LIT_SERVER_ID = "virtual:minimal-html/lit-server";
const DEFAULT_LIT_SSR_OUT_DIR = "dist/server";
const DEFAULT_LIT_SERVER_FILE = "server.js";
const DEFAULT_LIT_SSR_INFO_FILE = ".vite/ssr.json";

const LIT_IMPORT_PATTERN =
  /from\s+["'](lit|lit\/[^"']+|@lit\/[^"']+|@lit-labs\/[^"']+)["']/m;
const CUSTOM_ELEMENT_PATTERN = /customElement\s*\(/m;

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

  const litEnabled = !!options.lit && options.lit.enabled !== false;
  const litCfg = {
    hydrateId: options.lit?.hydrateVirtualId ?? DEFAULT_LIT_HYDRATE_ID,
    serverId: options.lit?.serverVirtualId ?? DEFAULT_LIT_SERVER_ID,
    ssrOutDir: options.lit?.ssrOutDir ?? DEFAULT_LIT_SSR_OUT_DIR,
    serverFile: options.lit?.serverFileName ?? DEFAULT_LIT_SERVER_FILE,
    ssrInfoFile: options.lit?.ssrInfoFileName ?? DEFAULT_LIT_SSR_INFO_FILE,
    extras: options.lit?.modules ?? [],
  };

  // discovered project-relative .ts/.js files that import lit
  let discoveredLitModules: string[] = [];

  const corePlugin: Plugin = {
    name: "minimal-html-vite",
    async config() {
      const inputs = await discoverInputs({
        scan,
        ignore,
        pattern,
        explicit,
      });

      if (litEnabled) {
        const candidates = await Promise.all(
          inputs.map(async (p) => ({
            path: p,
            isLit: p.startsWith("virtual:") ? false : await scanForLit(p),
          })),
        );
        discoveredLitModules = [
          ...new Set(candidates.filter((c) => c.isLit).map((c) => c.path)),
        ];
      }

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

      if (!litEnabled) {
        return { build: clientBuild, ...cssConfig };
      }

      const serverEntryName = litCfg.serverFile.replace(/\.[^./\\]+$/, "");
      return {
        ...cssConfig,
        environments: {
          client: { build: clientBuild },
          ssr: {
            build: {
              outDir: litCfg.ssrOutDir,
              emptyOutDir: false,
              rolldownOptions: {
                input: { [serverEntryName]: litCfg.serverId },
                output: { entryFileNames: "[name].js", format: "esm" as const },
              },
            },
            resolve: {
              noExternal: true,
            },
          },
        },
        builder: {
          sharedPlugins: true,
          async buildApp(builder: any) {
            await builder.build(builder.environments.client);
            await builder.build(builder.environments.ssr);
          },
        },
      };
    },
  };

  const plugins: Plugin[] = [corePlugin];

  if (litEnabled) {
    plugins.push({
      name: "minimal-html-vite:lit",
      apply: "build",
      resolveId(id: string) {
        if (id === litCfg.serverId || id === litCfg.hydrateId) {
          return "\0" + id;
        }
        return null;
      },
      load(id: string) {
        if (id === "\0" + litCfg.serverId) {
          return generateServerEntry(
            discoveredLitModules.map((p) => path.resolve(process.cwd(), p)),
            litCfg.extras,
          );
        }
        if (id === "\0" + litCfg.hydrateId) {
          return `import "@lit-labs/ssr-client/lit-element-hydrate-support.js";\n`;
        }
        return null;
      },
      generateBundle(this: any) {
        const envName = this.environment?.name;
        if (envName && envName !== "client") return;
        this.emitFile({
          type: "asset",
          fileName: litCfg.ssrInfoFile,
          source: JSON.stringify(
            {
              serverPath: litCfg.ssrOutDir,
              serverModule: litCfg.serverFile,
            },
            null,
            2,
          ),
        });
      },
    });
  }

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
      if (p.startsWith("virtual:")) return Promise.resolve(p);
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

async function scanForLit(path: string): Promise<boolean> {
  try {
    const content = await readFile(path, { encoding: "utf-8" });
    return (
      LIT_IMPORT_PATTERN.test(content) || CUSTOM_ELEMENT_PATTERN.test(content)
    );
  } catch {
    return false;
  }
}

function generateServerEntry(discovered: string[], extras: string[]): string {
  const imports = [
    ...discovered.map((p) => `import ${JSON.stringify(p)};`),
    ...extras.map((p) => `import ${JSON.stringify(p)};`),
  ].join("\n");

  return `import { render } from "@lit-labs/ssr";
import { html } from "lit";
${imports}

export async function renderHtml(strings, values, write, flush) {
  const templateStrings = Object.freeze(
    Object.defineProperty([...strings], "raw", {
      value: Object.freeze([...strings]),
    }),
  );
  const iterator = render(html(templateStrings, ...values));
  await renderIterator(iterator, write, flush);
}

async function renderIterator(iterator, write, flush) {
  for (const chunk of iterator) {
    if (typeof chunk === "string") {
      write(chunk);
    } else {
      await flush();
      await renderIterator(await chunk, write, flush);
    }
  }
}
`;
}

function escapeRegex(s: string): string {
  return s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

export { minimalHtml };
