# MinimalHtml

A high-performance, AOT-compatible HTML template library for .NET 10 that leverages C# `StringInterpolationHandler` and `System.IO.Pipelines` for efficient streaming HTML generation.

## Features

- **🚀 High Performance**: Direct `PipeWriter` streaming with minimal allocations
- **🔒 AOT Compatible**: Full support for Native AOT compilation
- **🛡️ XSS Protection**: Automatic HTML encoding for all interpolated values
- **🎨 IDE Support**: Full HTML syntax highlighting in JetBrains Rider, plus HTML tokenization, hover docs, and autocomplete in VS Code via the [HTML# extension](https://marketplace.visualstudio.com/items?itemName=flixcor.html-sharp)

## Quick Start

### Installation

```bash
dotnet add package MinimalHtml
dotnet add package MinimalHtml.AspNetCore  # For ASP.NET Core integration
```

### Basic Usage

```csharp
using MinimalHtml;

// Define a template as a static method or property
public static readonly Template<User> UserTemplate = 
    (writer, user) => writer.Html($"""
        <div class="user-card">
            <h2>{user.Name}</h2>
            <p>{user.Email}</p>
        </div>
        """);

// Use in ASP.NET Core
app.MapGet("/user/{id}", (int id, UserService users) =>
{
    var user = users.GetById(id);
    return Results.Html(UserTemplate, user);
});
```

## Security

All template interpolations are automatically HTML-encoded to prevent XSS attacks:

```csharp
var userInput = "<script>alert('xss')</script>";
writer.Html($"<p>User said: {userInput}</p>");
// Outputs: <p>User said: &lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt;</p>
```

## Performance

MinimalHtml is designed for maximum performance:

- **Streaming templates**: Improved Time to First Byte (TTFB) by sending content as it's generated ([example](https://sample.minimalhtml.net/any-order))
- **Zero allocation string literals**: Cached as UTF-8 byte arrays
- **Direct PipeWriter streaming**: No intermediate string building
- **Source generation**: Templates pre-compiled when possible
- **AOT friendly**: No runtime compilation or reflection

### Template Caching Setup

For optimal performance, enable UTF-8 byte array caching of string literals using the source generator:

1. Create a template cache class:
```csharp
using MinimalHtml;

public static partial class TemplateCache
{
    [FillTemplateCache]
    public static partial void Cache();
}
```

2. Initialize the cache in your Program.cs:
```csharp
// Initialize template cache for optimal performance (skip in debug for Hot Reload compatibility)
#if !DEBUG
TemplateCache.Cache();
#endif

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => Results.Html($"<h1>Hello World!</h1>"));
app.Run();
```

The source generator will automatically populate this class with cached UTF-8 byte arrays for all string literals found in your templates, significantly improving performance by eliminating encoding overhead.

## Vite integration

`MinimalHtml.Vite` (NuGet) + `@minimalhtml/vite` (npm) pair the .NET runtime with a Vite-built frontend. The NuGet package reads Vite's `manifest.json` to resolve hashed asset URLs, integrity hashes, and import maps. The npm plugin discovers entry points from your `.cs` files via marker comments and emits the manifest with SHA-384 SRI hashes.

### Install

```bash
dotnet add package MinimalHtml.Vite
pnpm add -D @minimalhtml/vite vite
```

### Vite config

```ts
// vite.config.ts
import { defineConfig } from "vite";
import minimalHtml from "@minimalhtml/vite";

export default defineConfig({
  plugins: [minimalHtml()],
});
```

### Program.cs

```csharp
using MinimalHtml.Vite;

builder.Services.RegisterViteAssets(importmapPath: ".vite/importmap.json");
```

`RegisterViteAssets` registers a `GetAsset` delegate (resolves `id → Asset { Src, Integrity, Imports }`) and a `WriteImportMap` delegate (emits the `<script type="importmap">` tag).

### Marker convention

Tag any string literal that names an asset with `/*vite*/`. The plugin scans `.cs` files for the marker and feeds matching literals to Vite's input list.

```csharp
public static Template Head = Assets.Style(/*vite*/"styles/main.css");
public static Template Init = Assets.Script(/*vite*/"scripts/main.ts");
```

`Assets.Script` / `Assets.Style` here are application-level helpers around `GetAsset` — see the sample app for examples that emit `<script integrity="..." crossorigin="anonymous">` and preload `<link>` tags from imports.

See [`@minimalhtml/vite` README](npm/vite/README.md) for plugin options (custom marker, scan globs, import-map and integrity tuning) and the [sample app](samples/MinimalHtml.Sample) for a working setup.

## Benchmarks

Rendering a list of 100 products (based on the [Fluid benchmark suite](https://github.com/sebastienros/fluid/tree/main/Fluid.Benchmarks)):

| Template engine | Mean     | Allocated |
|-----------------|---------:|----------:|
| Fluid           | 346.4 μs |  32.77 KB |
| MinimalHtml     | 117.8 μs |   6.57 KB |

MinimalHtml renders ~2.9× faster and allocates ~5× less memory.

<sub>BenchmarkDotNet v0.15.2, .NET 10.0.6, Windows 11, X64 RyuJIT AVX2. `InvocationCount=1, UnrollFactor=1`.</sub>

Run the included benchmarks yourself:

```bash
dotnet run --project benchmark/MinimalHtml.Benchmarks.csproj -c Release
```

## Requirements

- .NET 10.0 or later
- C# 14.0 or later

## License

This project is licensed under the MIT License. See the LICENSE file for details.