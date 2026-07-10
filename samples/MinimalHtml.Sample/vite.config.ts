import { defineConfig } from "vite";
import minimalHtml from "@minimalhtml/vite";

export default defineConfig(() => ({
  appType: "custom",
  css: {
    transformer: "postcss",
  },
  plugins: [minimalHtml({ lit: {}, cssModules: true })],
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
