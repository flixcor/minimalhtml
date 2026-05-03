import type { Plugin } from "vite";
import { glob } from "tinyglobby";
import { readFile } from "node:fs/promises";
import { resolve } from "node:path";

const VIRTUAL_ID = "virtual:minimalhtml-lit-ssr";
const RESOLVED_ID = "\0" + VIRTUAL_ID;

let globalLogged = false;

export interface MinimalHtmlLitOptions {
  /** Globs to scan for Lit components. Default: ["**\/*.{ts,tsx,mts,cts}"]. */
  scan?: string[];
  /** Globs to ignore. Default: node_modules, dist, wwwroot, bin, obj. */
  ignore?: string[];
  /** Extra files to include in SSR entry regardless of scan. Paths relative to cwd. */
  extraEntries?: string[];
  /** SSR build output directory. Default: "dist/server". */
  ssrOutDir?: string;
  /** SSR entry chunk name. Default: "server" (emits server.js). */
  ssrEntryName?: string;
}

const DEFAULT_SCAN = ["**/*.{ts,tsx,mts,cts}"];
const DEFAULT_IGNORE = [
  "**/node_modules/**",
  "**/dist/**",
  "**/wwwroot/**",
  "**/bin/**",
  "**/obj/**",
];
const LIT_IMPORT_RE =
  /from\s+['"](lit|lit\/[^'"]+|@lit-labs\/[^'"]+|@lit\/[^'"]+)['"]/;
const LIT_DEFINE_RE =
  /@customElement\s*\(|customElements\.define\s*\(|extends\s+LitElement\b/;

export default function minimalHtmlLit(
  options: MinimalHtmlLitOptions = {},
): Plugin {
  const scan = options.scan ?? DEFAULT_SCAN;
  const ignore = options.ignore ?? DEFAULT_IGNORE;
  const extra = options.extraEntries ?? [];
  const ssrOutDir = options.ssrOutDir ?? "dist/server";
  const ssrEntryName = options.ssrEntryName ?? "server";

  let discovered: string[] = [];

  async function rediscover(cwd: string) {
    discovered = await discover(scan, ignore, cwd);
  }

  return {
    name: "minimalhtml-lit",
    async config() {
      await rediscover(process.cwd());
      if (!globalLogged) {
        // eslint-disable-next-line no-console
        console.log(
          `[minimalhtml-lit] discovered ${discovered.length + extra.length} component file(s) for SSR`,
        );
        globalLogged = true;
      }
      return {
        environments: {
          ssr: {
            build: {
              ssr: true,
              outDir: ssrOutDir,
              emptyOutDir: true,
              rolldownOptions: {
                input: { [ssrEntryName]: VIRTUAL_ID },
                output: {
                  format: "esm" as const,
                  entryFileNames: "[name].js",
                  chunkFileNames: "chunks/[name]-[hash].js",
                },
              },
            },
            resolve: {
              noExternal: true,
            },
          },
        },
      };
    },
    resolveId(id) {
      if (id === VIRTUAL_ID) return RESOLVED_ID;
      return null;
    },
    async load(id) {
      if (id !== RESOLVED_ID) return null;
      const cwd = process.cwd();
      const all = [...new Set([...discovered, ...extra])]
        .map((p) => resolve(cwd, p).replace(/\\/g, "/"));
      const lines = [
        `export { renderHtml } from "@minimalhtml/vite/lit/server-runtime";`,
        ...all.map((p) => `import ${JSON.stringify(p)};`),
      ];
      return lines.join("\n") + "\n";
    },
    configureServer(server) {
      const refresh = async (file: string) => {
        if (!/\.(?:ts|tsx|mts|cts)$/.test(file)) return;
        const before = new Set(discovered);
        await rediscover(process.cwd());
        const changed =
          discovered.length !== before.size ||
          discovered.some((f) => !before.has(f));
        if (changed) {
          const mod = server.environments.ssr?.moduleGraph.getModuleById(
            RESOLVED_ID,
          );
          if (mod) server.environments.ssr?.moduleGraph.invalidateModule(mod);
        }
      };
      server.watcher.on("add", refresh);
      server.watcher.on("unlink", refresh);
      server.watcher.on("change", refresh);
    },
  };
}

async function discover(
  scan: string[],
  ignore: string[],
  cwd: string,
): Promise<string[]> {
  const files = await glob(scan, { ignore, cwd });
  const matches = await Promise.all(
    files.map(async (file) => {
      const content = await readFile(resolve(cwd, file), "utf8");
      const stripped = stripComments(content);
      if (!LIT_IMPORT_RE.test(stripped)) return null;
      if (!LIT_DEFINE_RE.test(stripped)) return null;
      return file;
    }),
  );
  return matches.filter((f): f is string => f !== null).sort();
}

function stripComments(src: string): string {
  return src.replace(/\/\*[\s\S]*?\*\//g, "").replace(/\/\/[^\n]*/g, "");
}

export { minimalHtmlLit };
