import { defineConfig, normalizePath } from 'vite'
import glob from 'fast-glob'
import { readFile, stat } from "fs/promises";

const findSrc = /Assets\.[A-Za-z]+:~?\/?([^}]+)}/g;
const findSrc2 = /AssetResolver\.Resolve\("~?\/?([^"]+)"/g;

async function getInputs() {
  const files = await glob(["./Pages/**/*.cs", "./Components/**/*.cs", "./Layouts/**/*.cs"])
  const promises = files.map((file) =>
    readFile(file, { encoding: "utf-8" }).then((content) =>
      [
        ...content.matchAll(findSrc),
        ...content.matchAll(findSrc2)
      ].map(([, group]) => group)
    )
  );
  const all = await Promise.all(promises);
  const distinct = [...new Set(all.flat())];
  const exists = (await Promise.all(distinct.map(x=> stat(x).then(() => x).catch(() => false as const)))).filter(Boolean)

  return Object.fromEntries(exists.map(x => [x, x]));
}

export default defineConfig({
  appType: 'custom',
  build: {
    // generate .vite/manifest.json in outDir
    manifest: true,
    modulePreload: false,
    sourcemap: true,
    rollupOptions: {
      input: await getInputs(),
      output: {
        entryFileNames: (x) => {
          if (x.facadeModuleId && normalizePath(x.facadeModuleId).startsWith(normalizePath(import.meta.dirname)))
            return normalizePath(
              x.facadeModuleId.substring(import.meta.dirname.length + 1)
            );
          return "assets/[name].js";
        },
        chunkFileNames: "assets/[name].js",
        assetFileNames: "assets/../[name].[ext]",
      },
    },
    outDir: 'wwwroot',
    emptyOutDir: true,
    // cssMinify: 'lightningcss'
  },
  // css: {
  //     transformer: 'lightningcss',
  //     lightningcss: {
  //         targets: browserslistToTargets(browserslist('>= 0.25%')),
  //         drafts: {
  //             customMedia: true
  //         }
  //     }
  // }
})
