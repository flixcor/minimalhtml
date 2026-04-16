import { render, type RenderResult } from "@lit-labs/ssr";
import { html } from "lit";
import "./Pages/Lit.ts";

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
      const thing = await chunk;
      await renderIterator(thing, write, flush);
    }
  }
}

export async function renderHtml(
  strings: TemplateStringsArray,
  values: unknown[],
  write: Write,
  flush: Flush,
) {
  // Make it look like a real tagged template strings array
  const templateStrings = Object.freeze(
    Object.defineProperty([...strings], "raw", {
      value: Object.freeze([...strings]),
    }),
  ) as TemplateStringsArray;
  const iterator = render(html(templateStrings, ...values));
  await renderIterator(iterator, write, flush);
}
