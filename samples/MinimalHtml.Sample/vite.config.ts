import { defineConfig } from "vite";
import glob from "fast-glob";
import { readFile, stat } from "fs/promises";
import { writeFileSync } from "node:fs";
//import jit from "lightningcss-jit-props"
//import { resolve } from "node:path"

const findSrc = /Assets\.[A-Za-z]+:~?\/?([^}]+)}/g;

async function getInputs() {
  const files = await glob([
    "./Pages/**/*.cs",
    "./Components/**/*.cs",
    "./Layouts/**/*.cs",
  ]);
  const promises = files.map((file) =>
    readFile(file, { encoding: "utf-8" }).then((content) =>
      [...content.matchAll(findSrc)].map(([, group]) => group),
    ),
  );
  const all = await Promise.all(promises);
  const distinct = [...new Set(all.flat())];
  const exists = (
    await Promise.all(
      distinct.map((x) =>
        stat(x)
          .then(() => x)
          .catch(() => false as const),
      ),
    )
  ).filter((x) => typeof x === "string");

  return exists;
}

const input = await getInputs();

console.warn(...input);
export default defineConfig({
  appType: "custom",

  css: {
    // transformer: 'lightningcss',
    transformer: "postcss",
    modules: {
      getJSON: (cssFileName, json) =>
        writeFileSync(cssFileName + ".json", JSON.stringify(json)),
    },
    // lightningcss: {
    //   visitor: jit({
    //     files: [
    //       resolve(__dirname, 'node_modules/open-props/open-props.min.css'),
    //     ]
    //   }),
    //   errorRecovery: true
    // }
  },
  build: {
    // generate .vite/manifest.json in outDir
    manifest: true,
    modulePreload: false,
    sourcemap: true,
    rolldownOptions: {
      experimental: {
        chunkImportMap: {
          baseUrl: "/",
          fileName: ".vite/importmap.json",
        },
      },
      input,
      output: {
        entryFileNames: (c) =>
          c.name.startsWith("serviceworker") ? "[name]-[hash].js" : "assets/[name]-[hash].js",
      },
    },
    outDir: "wwwroot",
    // emptyOutDir: false,
    assetsInlineLimit: -1,
  },
});
