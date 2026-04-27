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
  ssr: {
    noExternal: true,
  },
  plugins: [minimalHtml()],
  build: {
    outDir: "wwwroot",
    assetsInlineLimit: -1,
    rolldownOptions: {
      output: {
        format: "esm" as const,
        entryFileNames: (c) =>
          c.name.startsWith("server")
            ? "[name].js"
            : c.name.startsWith("serviceworker")
              ? "[name]-[hash].js"
              : "assets/[name]-[hash].js",
      },
    },
  },
}));
