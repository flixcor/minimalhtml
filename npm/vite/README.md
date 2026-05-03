# @minimalhtml/vite

Vite plugin for [MinimalHtml](https://github.com/flixcor/minimalhtml). Pairs with the `MinimalHtml.Vite` NuGet package.

## What it does

- Scans `.cs` files for marker comments to discover entry points
- Sets `build.manifest = true` so the .NET side can read `.vite/manifest.json`
- Disables `modulePreload` (server emits preload `<link>` tags from manifest imports)
- Enables Rolldown `chunkImportMap` to emit `.vite/importmap.json`
- Sets a sensible default `entryFileNames` (`assets/[name]-[hash].js`)
- Emits Subresource Integrity hashes (SHA-384) into the manifest via [`vite-plugin-manifest-sri`](https://github.com/ElMassimo/vite-plugin-manifest-sri)

## Install

```bash
pnpm add -D @minimalhtml/vite vite
```

Requires Vite 8+ (Rolldown). The `chunkImportMap` feature is Rolldown experimental.

## Usage

```ts
// vite.config.ts
import { defineConfig } from "vite";
import minimalHtml from "@minimalhtml/vite";

export default defineConfig({
  plugins: [minimalHtml()],
});
```

## Marker convention

Prepend `/*vite*/` to any string literal that names an asset. The plugin scans `.cs` files for the pattern and feeds the literal to Vite's input list.

```csharp
return Assets.Script(/*vite*/"src/main.ts");
return Assets.Style(/*vite*/"src/styles/main.css");
```

Default scan glob: `./**/*.cs`. Default ignored: `bin/`, `obj/`, `node_modules/`.

## Options

```ts
minimalHtml({
  scan: ["./**/*.cs"],            // globs to scan
  marker: "vite",                 // marker token
  ignore: ["**/bin/**", ...],     // glob ignores
  inputs: ["./extra.ts"],         // extra explicit inputs
  entryFileNames: "assets/[name]-[hash].js",
  disableImportMap: false,        // skip chunkImportMap emission
  importMapBaseUrl: "/",
  importMapFileName: ".vite/importmap.json",
  disableIntegrity: false,        // skip SRI hash emission
  integrityAlgorithms: ["sha384"], // hash algorithms
  integrityManifestPaths: [".vite/manifest.json"],
});
```

## Subresource Integrity

Each manifest entry receives an `integrity` field (`sha384-<base64>` by default). The .NET side reads it into `Asset.Integrity`; rendering helpers should emit it as `<script integrity="...">` / `<link integrity="...">`.

When `integrity` is set, the browser requires `crossorigin` on the tag. Same-origin assets are fine without it; for assets served from a different origin (CDN, separate static host), emit `crossorigin="anonymous"` together with `integrity`. Example helper logic:

```csharp
{IfTrueish("integrity", asset.Integrity)}
{IfTrueish("crossorigin", asset.Integrity is null ? null : "anonymous")}
```

Disable SRI emission entirely with `disableIntegrity: true` if your deploy target doesn't need it.

## Pairing with the .NET side

```csharp
// Program.cs
builder.Services.RegisterViteAssets(importmapPath: ".vite/importmap.json");
```

See the [main repo README](https://github.com/flixcor/minimalhtml) for the full picture.

## Lit SSR (subpath)

Optional integration with [Lit](https://lit.dev) for server-side rendering custom elements via [@lit-labs/ssr](https://www.npmjs.com/package/@lit-labs/ssr). The `MinimalHtml.Lit` NuGet package renders the bundle in [Jint](https://github.com/sebastienros/jint) on the .NET side.

### Install

```bash
pnpm add lit @lit-labs/ssr @lit-labs/ssr-client
```

### Plugin

```ts
// vite.config.ts
import { defineConfig } from "vite";
import minimalHtml from "@minimalhtml/vite";
import minimalHtmlLit from "@minimalhtml/vite/lit/plugin";

export default defineConfig({
  plugins: [minimalHtml(), minimalHtmlLit()],
});
```

`vite build --app` produces both the client bundle (`wwwroot/`) and the SSR bundle (`dist/server/server.js`).

### Discovery

The plugin scans `**/*.{ts,tsx,mts,cts}` for files that both import from `lit` (or `@lit-labs/*`) and define a custom element (`@customElement`, `customElements.define`, or `extends LitElement`). Each match is side-effect-imported by the SSR entry. No marker comment required.

### Subpaths

| Subpath | Use |
|---|---|
| `@minimalhtml/vite/lit/plugin` | Vite plugin (above) |
| `@minimalhtml/vite/lit/server-runtime` | `renderHtml(strings, values, write, flush)` + Jint shims (`window`, `customElements`, `btoa`). The plugin's virtual entry re-exports this — you only import it directly if writing your own SSR entry. |
| `@minimalhtml/vite/lit/hydrate` | Client-side hydration support. Reference via `Assets.Script(/*vite*/"@minimalhtml/vite/lit/hydrate")` to hydrate SSR'd components. |

### SSR-only components

A component file that imports `lit` but is **not** referenced from any `/*vite*/` client marker is bundled into the SSR runtime only — zero JavaScript ships to the browser. The element renders via Declarative Shadow DOM. No hydration happens, so event bindings (`@click=`) are inert. Suitable for static badges, layouts, formatted output, etc.

### .NET side

```csharp
// Program.cs
using MinimalHtml.Lit;

builder.Services.AddLitRenderer();
```

Defaults to `{AppContext.BaseDirectory}/dist/server/server.js`. Override with `AddLitRenderer(serverPath: ..., serverModule: ...)`.

```csharp
// Pages/Lit.cs
private static readonly Template s_body = static page => page.Html($"""
    {static w => w.Lit(/*lang=html*/$"<my-element count={5}>Hello</my-element>")}
""");
```

Make sure `dist/server/**` is copied to the publish output:

```xml
<ItemGroup>
  <Content Include="dist\server\**" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

## License

MIT
