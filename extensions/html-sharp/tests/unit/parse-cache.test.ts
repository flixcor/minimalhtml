import * as assert from "node:assert/strict";
import { ParseCache } from "../../src/parse-cache";

// Stub for vscode.TextDocument — no extension host required.
function makeDoc(uri: string, version: number, text: string) {
  return {
    uri: { toString: () => uri },
    version,
    getText: () => text,
  } as unknown as import("vscode").TextDocument;
}

const HTML_TEXT = 'var x = /*lang=html*/ "<div></div>";';

describe("ParseCache", () => {
  it("cache miss: parses document and returns regions", () => {
    const cache = new ParseCache();
    const doc = makeDoc("file:///test.cs", 1, HTML_TEXT);
    const regions = cache.getRegions(doc);
    assert.ok(Array.isArray(regions));
    assert.ok(regions.length > 0, "expected HTML region to be detected");
  });

  it("cache hit: returns the same array reference without re-parsing", () => {
    const cache = new ParseCache();
    const doc = makeDoc("file:///test.cs", 1, HTML_TEXT);
    const first = cache.getRegions(doc);
    const second = cache.getRegions(doc);
    assert.strictEqual(first, second, "cache hit must return the exact same array reference");
  });

  it("stale entry: version change replaces cached entry with fresh parse", () => {
    const cache = new ParseCache();
    const v1 = makeDoc("file:///test.cs", 1, HTML_TEXT);
    const first = cache.getRegions(v1);

    const v2 = makeDoc("file:///test.cs", 2, 'var y = /*lang=html*/ "<span></span>";');
    const second = cache.getRegions(v2);

    assert.notStrictEqual(first, second, "version change must produce a new array reference");
  });

  it("invalidate: removes entry so the next call re-parses", () => {
    const cache = new ParseCache();
    const doc = makeDoc("file:///test.cs", 1, HTML_TEXT);
    const first = cache.getRegions(doc);
    cache.invalidate(doc);
    const second = cache.getRegions(doc);
    assert.notStrictEqual(first, second, "after invalidate, a fresh parse must produce a new reference");
  });

  it("invalidate: no-op when document has no cached entry", () => {
    const cache = new ParseCache();
    const doc = makeDoc("file:///never-parsed.cs", 1, "");
    assert.doesNotThrow(() => cache.invalidate(doc));
  });

  it("independent documents: each URI is cached separately", () => {
    const cache = new ParseCache();
    const doc1 = makeDoc("file:///a.cs", 1, HTML_TEXT);
    const doc2 = makeDoc("file:///b.cs", 1, 'var y = /*lang=html*/ "<span></span>";');

    cache.getRegions(doc1);
    const cached2 = cache.getRegions(doc2);

    // Invalidating doc1 must not evict doc2's entry.
    cache.invalidate(doc1);
    const cached2Again = cache.getRegions(doc2);
    assert.strictEqual(cached2, cached2Again, "invalidating doc1 must not evict doc2 entry");
  });

  it("returns empty array for document with no HTML content", () => {
    const cache = new ParseCache();
    const doc = makeDoc("file:///plain.cs", 1, "class Foo {}");
    const regions = cache.getRegions(doc);
    assert.deepEqual(regions, []);
  });

  // US1: second getRegions call for same version returns cached reference (parseHtmlRegions not called again)
  it("[US1] same document version: second call returns cached reference proving single parse", () => {
    const cache = new ParseCache();
    const doc = makeDoc("file:///spy.cs", 1, HTML_TEXT);
    const a = cache.getRegions(doc);
    const b = cache.getRegions(doc);
    assert.strictEqual(
      a,
      b,
      "same version: second getRegions must return cached reference (no re-parse)",
    );
  });
});
