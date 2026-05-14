import { defineConfig } from "vite";
import { writeFileSync } from "node:fs";
import minimalHtml from "@minimalhtml/vite";

export default defineConfig(() => ({
  appType: "custom",
  css: {
    transformer: "postcss",
    modules: {
      getJSON: (cssFileName, json) =>
        writeFileSync(cssFileName + ".json", JSON.stringify(json)),
    },
  },
  plugins: [minimalHtml({ lit: {} })],
  build: {
    outDir: "wwwroot",
    assetsInlineLimit: -1,
    rolldownOptions: {
      output: {
        format: "esm" as const,
        entryFileNames: (c) =>
          c.name.startsWith("serviceworker")
            ? "[name]-[hash].js"
            : "assets/[name]-[hash].js",
      },
    },
  },
}));
