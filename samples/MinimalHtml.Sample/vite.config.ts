import { defineConfig } from "vite";
import { writeFileSync } from "node:fs";
import minimalHtml from "@minimalhtml/vite";
import minimalHtmlLit from "@minimalhtml/vite/lit/plugin";

export default defineConfig(() => ({
  appType: "custom",
  css: {
    transformer: "postcss",
    modules: {
      getJSON: (cssFileName, json) =>
        writeFileSync(cssFileName + ".json", JSON.stringify(json)),
    },
  },
  plugins: [minimalHtml(), minimalHtmlLit()],
  build: {
    outDir: "wwwroot",
    assetsInlineLimit: -1,
    rolldownOptions: {
      output: {
        format: "esm" as const,
        entryFileNames: (c) => {
          const base = (c.name.split("/").pop() ?? c.name).replace(
            /\.(?:tsx?|jsx?|mjs|cjs)$/,
            "",
          );
          return base.startsWith("serviceworker")
            ? `${base}-[hash].js`
            : `assets/${base}-[hash].js`;
        },
      },
    },
  },
}));
