// v5 unit tests for HtmlCompletionProvider — TC-001 through TC-010.
// Uses getCompletionsForOffset() which is pure-Node (no vscode API) for testability.
import * as assert from "node:assert/strict";
import { getCompletionsForOffset } from "../../src/completion-core";
import { parseHtmlRegions } from "../../src/marker-parser";

describe("HtmlCompletionProvider — getCompletionsForOffset", () => {
  describe("US1 — element name completion", () => {
    it("TC-001: offset on element name after '<' returns element completions including 'div'", () => {
      const text = 'string s = /*lang=html*/ "<div>";';
      const regions = parseHtmlRegions(text);
      const bodyStart = regions[0].bodyStart; // position of '<'
      const result = getCompletionsForOffset(text, bodyStart + 1); // on 'd'
      assert.ok(result, "expected CompletionResult");
      assert.ok(result.items.some(i => i.label === "div"), "items must include 'div'");
    });

    it("TC-002: offset on partial element name returns matching completions including 'p'", () => {
      const text = 'string s = /*lang=html*/ "<p>hello</p>";';
      const regions = parseHtmlRegions(text);
      const bodyStart = regions[0].bodyStart;
      const result = getCompletionsForOffset(text, bodyStart + 1); // on 'p'
      assert.ok(result, "expected CompletionResult");
      assert.ok(result.items.some(i => i.label === "p"), "items must include 'p'");
    });

    it("TC-005: offset on element name in closing tag returns closing tag completions", () => {
      // Incomplete closing tag (no '>') so the LS knows we're mid-tag
      const text = 'string s = /*lang=html*/ "<p></p";';
      const regions = parseHtmlRegions(text);
      const bodyStart = regions[0].bodyStart;
      // virtual: "<p></p" — 'p' in '</p' at virtual position 5
      const result = getCompletionsForOffset(text, bodyStart + 5);
      assert.ok(result, "expected CompletionResult");
      assert.ok(
        result.items.some(i => i.label === "p" || i.label === "/p"),
        "closing tag items must include 'p' or '/p'",
      );
    });

    it("TC-007: offset inside interpolation hole returns undefined", () => {
      const text = 'string s = /*lang=html*/ $"<p>{name}</p>";';
      const regions = parseHtmlRegions(text);
      const region = regions[0];
      // hole.start is position of '{', hole.start+1 is 'n' of 'name' — inside the hole
      const holeInnerOffset = region.holes[0].start + 1;
      const result = getCompletionsForOffset(text, holeInnerOffset);
      assert.equal(result, undefined, "offset inside hole must return undefined");
    });

    it("TC-008: offset in non-HTML plain string returns undefined", () => {
      const text = 'string s = "hello <div>";';
      // no HTML region — no marker or Html() call
      const result = getCompletionsForOffset(text, 18);
      assert.equal(result, undefined, "non-HTML string must return undefined");
    });

    it("TC-009: empty region body does not crash", () => {
      const text = 'string s = /*lang=html*/ "";';
      assert.doesNotThrow(() => {
        const regions = parseHtmlRegions(text);
        if (regions.length > 0) {
          // bodyStart === bodyEnd for empty region: offset is outside region, returns undefined
          getCompletionsForOffset(text, regions[0].bodyStart);
        }
      }, "empty region must not throw");
    });
  });

  describe("US2 — attribute name and value completion", () => {
    it("TC-003: offset after element name space returns attribute name completions", () => {
      // Two spaces: second space (pos 8) is clearly "in attribute position, no prefix" for button
      const text = 'string s = /*lang=html*/ "<button  ";';
      const regions = parseHtmlRegions(text);
      const bodyStart = regions[0].bodyStart;
      // virtual: "<button  " — second space at virtual position 8
      const result = getCompletionsForOffset(text, bodyStart + 8);
      assert.ok(result, "expected CompletionResult");
      assert.ok(result.items.some(i => i.label === "type"), "items must include 'type'");
      assert.ok(result.items.some(i => i.label === "disabled"), "items must include 'disabled'");
    });

    it("TC-004: offset inside attribute value quotes returns value completions", () => {
      // Using single-quoted HTML attribute values (valid HTML5, no C# escape needed)
      const text = "string s = /*lang=html*/ \"<input type=''>\";";
      const regions = parseHtmlRegions(text);
      const bodyStart = regions[0].bodyStart;
      // virtual: "<input type=''>" — closing ' is at virtual position 13
      const result = getCompletionsForOffset(text, bodyStart + 13);
      assert.ok(result, "expected CompletionResult");
      assert.ok(result.items.some(i => i.label === "text"), "items must include 'text'");
    });

    it("TC-006: Html() trigger — offset after element name space returns attribute completions", () => {
      const text = 'string s = Html("<div >");';
      const regions = parseHtmlRegions(text);
      const bodyStart = regions[0].bodyStart;
      // virtual: "<div >" — space at virtual position 4
      const result = getCompletionsForOffset(text, bodyStart + 4);
      assert.ok(result, "expected CompletionResult");
      assert.ok(result.items.length > 0, "should return attribute completions via Html() trigger");
    });

    it("TC-010: unknown/custom element does not crash and returns a result", () => {
      const text = 'string s = /*lang=html*/ "<my-widget >";';
      const regions = parseHtmlRegions(text);
      const bodyStart = regions[0].bodyStart;
      // space after 'my-widget' is at virtual position 11
      assert.doesNotThrow(() => {
        getCompletionsForOffset(text, bodyStart + 11);
      }, "unknown element must not throw");
    });
  });
});
