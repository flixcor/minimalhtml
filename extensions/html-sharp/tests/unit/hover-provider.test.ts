// v4 unit tests for HtmlHoverProvider — TH-001 through TH-011.
// Uses getHoverForOffset() which is pure-Node (no vscode API) for testability.
import * as assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import * as path from "node:path";
import { getHoverForOffset } from "../../src/hover-core";

const REPO_ROOT = path.resolve(__dirname, "..", "..", "..");
const FIXTURE = (...p: string[]) =>
  readFileSync(path.resolve(REPO_ROOT, "tests", "fixtures", ...p), "utf8");

describe("HtmlHoverProvider — getHoverForOffset", () => {
  describe("US1 — element tag hover", () => {
    it("TH-001: offset on 'p' inside opening <p> tag returns a hover with element description", () => {
      // hover-regular.cs: `/*lang=html*/ "<p>Hello</p>"`
      // region 0: bodyStart=196, bodyEnd=208; 'p' in <p> = offset 197
      const text = FIXTURE("positive", "hover-regular.cs");
      const result = getHoverForOffset(text, 197);
      assert.ok(result, "expected a Hover result for <p> element");
      assert.ok(
        result.contents.length > 0,
        "hover contents must be non-empty",
      );
    });

    it("TH-002: offset on 'p' inside closing </p> tag returns the same hover", () => {
      // region 0: 'p' in </p> = offset 206
      const text = FIXTURE("positive", "hover-regular.cs");
      const r1 = getHoverForOffset(text, 197);
      const r2 = getHoverForOffset(text, 206);
      assert.ok(r2, "expected a Hover result for </p> element");
      assert.ok(r1 && r2, "both opening and closing tags should return a hover");
    });

    it("TH-003: offset inside an interpolation hole body returns undefined", () => {
      // hover-interp.cs: `/*lang=html*/ $"<p>{name}</p>"`
      // hole innerStart=196, innerEnd=200; 'n' of 'name' = 196
      const text = FIXTURE("positive", "hover-interp.cs");
      const result = getHoverForOffset(text, 196);
      assert.equal(result, undefined, "hole body must return undefined");
    });

    it("TH-004: offset on 'p' in text span of an interpolated string returns a hover", () => {
      // hover-interp.cs region 0: bodyStart=192; '<p>' starts at 192, 'p' at 193
      const text = FIXTURE("positive", "hover-interp.cs");
      const result = getHoverForOffset(text, 193);
      assert.ok(result, "text-span position in interpolated string must return hover");
      assert.ok(result.contents.length > 0, "hover contents must be non-empty");
    });

    it("TH-005: offset inside a non-HTML plain string returns undefined", () => {
      // hover-non-html.cs: `"This is just a <string>"` — no HTML region
      // 's' in '<string>' = offset 196 (inside the plain string literal)
      const text = FIXTURE("negative", "hover-non-html.cs");
      const result = getHoverForOffset(text, 196);
      assert.equal(result, undefined, "non-HTML string must return undefined");
    });
  });

  describe("US2 — attribute hover", () => {
    it("TH-006: offset on 'disabled' attribute name returns a hover", () => {
      // hover-attribute.cs region 0: bodyStart=179; 'disabled' starts at 187
      const text = FIXTURE("positive", "hover-attribute.cs");
      const result = getHoverForOffset(text, 187);
      assert.ok(result, "expected hover for 'disabled' attribute");
      assert.ok(result.contents.length > 0, "hover contents must be non-empty");
    });

    it("TH-007: offset on element name in a method-triggered string returns a hover", () => {
      // hover-regular.cs region 2 (Html(...)): 'article' starts at 282
      const text = FIXTURE("positive", "hover-regular.cs");
      const result = getHoverForOffset(text, 282);
      assert.ok(result, "expected hover for <article> element via method trigger");
      assert.ok(result.contents.length > 0, "hover contents must be non-empty");
    });

    it("TH-008: offset on unknown/custom element name returns undefined (graceful no-op)", () => {
      // hover-unknown-element.cs: 'my-widget' starts at 202 (bodyStart=201, 'm' at 202)
      const text = FIXTURE("negative", "hover-unknown-element.cs");
      const result = getHoverForOffset(text, 202);
      assert.equal(result, undefined, "unknown element must return undefined without error");
    });

    it("TH-009: empty region body does not crash", () => {
      const text = 'class C { string s = /*lang=html*/ ""; }';
      const emptyBodyOffset = text.indexOf('"') + 1; // right at bodyStart (= closing ")
      assert.doesNotThrow(() => {
        getHoverForOffset(text, emptyBodyOffset);
      }, "empty body must not throw");
    });

    it("TH-010: offset at exact bodyStart does not crash", () => {
      // region 0 bodyStart=196 = '<' of '<p>Hello</p>'
      const text = FIXTURE("positive", "hover-regular.cs");
      assert.doesNotThrow(() => {
        getHoverForOffset(text, 196);
      }, "bodyStart offset must not throw");
    });

    it("TH-011: offset at bodyEnd-1 does not crash", () => {
      // region 0 bodyEnd=208, bodyEnd-1=207 = '>' of '</p>'
      const text = FIXTURE("positive", "hover-regular.cs");
      assert.doesNotThrow(() => {
        getHoverForOffset(text, 207);
      }, "bodyEnd-1 offset must not throw");
    });
  });
});
