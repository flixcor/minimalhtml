import "./internal/jint-shims.js";
import { render, type RenderResult } from "@lit-labs/ssr";
import { html } from "lit";

type Write = (chunk: string) => void;
type Flush = () => Promise<void>;

async function renderIterator(
  iterator: RenderResult,
  write: Write,
  flush: Flush,
) {
  for (const chunk of iterator) {
    if (typeof chunk === "string") {
      write(chunk);
    } else {
      await flush();
      const inner = await chunk;
      await renderIterator(inner, write, flush);
    }
  }
}

export async function renderHtml(
  strings: TemplateStringsArray,
  values: unknown[],
  write: Write,
  flush: Flush,
) {
  const templateStrings = Object.freeze(
    Object.defineProperty([...strings], "raw", {
      value: Object.freeze([...strings]),
    }),
  ) as TemplateStringsArray;
  const iterator = render(html(templateStrings, ...values));
  await renderIterator(iterator, write, flush);
}
