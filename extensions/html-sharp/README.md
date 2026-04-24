# HTML#

A tiny VS Code extension that colors HTML embedded in C# string literals.

There are two ways to activate HTML coloring inside a C# string literal:

1. **Marker trigger**: prefix the string with a `lang=html` comment.
2. **Method trigger**: pass the string as the first argument of `Html(…)`.

Either way, the string body is tokenized by VS Code's bundled HTML grammar
instead of Roslyn's flat string coloring. For interpolated literals, every
**text span** (the parts outside interpolation holes) receives HTML coloring,
while every **hole body** retains its normal C# semantic tokens. No settings
change, no theme override — works with
`editor.semanticHighlighting.enabled` at its default value of `true`.

## Installation

From a packaged `.vsix`:

```bash
code --install-extension minimalhtml-extension-<version>.vsix
```

Or install from the VS Code Marketplace once published.

### Building from source

```bash
npm install
npm run build                 # bundle src/extension.ts → dist/extension.js
npm test                      # tmgrammar + unit tests
npm run test:e2e              # launches a test VS Code instance
npm run package               # produces a .vsix
```

## Supported trigger syntax

### Marker trigger

All of the following, case-insensitively, directly before the string
literal (separated only by whitespace or newlines):

- `// lang=html`
- `// language=html`
- `/* lang=html */`
- `/* language=html */`

Internal whitespace inside the comment is tolerated (`/* Lang = Html */`).

### Method trigger

Pass the string as the **first argument** of a method named exactly `Html`:

```csharp
string html = Html("<p>Hello <b>world</b></p>");
string tmpl = Html($"<h1>{title}</h1>");
```

The method name is case-sensitive (`Html`, not `html` or `HTML`). Any
preceding identifier character (`HtmlHelper`, `myHtml`) prevents the
trigger. Optional whitespace between `Html` and `(` is tolerated.

## Supported string literal forms

### Non-interpolated (v1)

- Regular: `"..."`
- Verbatim: `@"..."`
- Raw (exactly 3 quotes): `"""..."""`

### Interpolated (v2)

- Regular interpolated: `$"..."` — holes use `{expr}` syntax
- Verbatim interpolated: `$@"..."` or `@$"..."` — holes use `{expr}` syntax
- Raw interpolated N=1: `$"""..."""` — holes use `{expr}` syntax
- Raw interpolated N=2: `$$"""..."""` — holes use `{{expr}}` syntax
- Raw interpolated N=3: `$$$"""..."""` — holes use `{{{expr}}}` syntax

Inside interpolated literals, text spans receive HTML coloring and each hole
body (`{expr}`, `{{expr}}`, or `{{{expr}}}`) retains its C# coloring unchanged.
Brace escapes (`{{` / `}}` at N=1, shorter-than-N runs at N≥2) render as HTML
text. Format specifiers (`:F2`) and alignment clauses (`,10`) inside holes are
part of the C# hole body and are not affected.

## Known limitations

- **Raw string literals with four or more opening quotes** (`""""...""""` etc.)
  are not supported.
- **Raw interpolated literals with N ≥ 4 leading `$` signs** (`$$$$"""..."""`)
  are not supported.
- **N ≥ 2 hole brace runs colored by bracket pair colorization**: at N ≥ 2,
  VS Code's bracket pair colorization paints each `{`/`}` by nesting depth
  regardless of TextMate scope. The brace runs therefore render with the usual
  depth palette rather than a uniform color.

## Hover documentation

Hovering over an HTML element name or attribute name inside an HTML-colored
string shows its description sourced from the HTML language service — the same
documentation you see in `.html` files. Hovering inside an interpolation hole
`{expr}` produces no HTML tooltip; VS Code's normal C# hover fires instead.

## Auto-complete

Typing `<`, space, `=`, `"`, `'`, or `/` inside an HTML-colored string triggers
HTML completions sourced from the HTML language service:

- **Element names** — after `<`, a list of standard HTML5 elements appears.
- **Closing tags** — after `</`, the matching open element is suggested.
- **Attribute names** — inside a tag after the element name, valid attributes appear.
- **Attribute values** — inside `=""` or `=''`, known values for that attribute appear.

Completions are not offered inside interpolation holes `{expr}`; C# IntelliSense
fires normally there. Unknown or custom element names (`<my-widget`) return global
attribute suggestions gracefully.
