import { parseHtmlRegions } from "../../src/marker-parser";

// T062 / T063: parser micro-benchmark extended to v2 interpolated corpus.
//
// Plan.md Performance Goals: parseHtmlRegions() must run in ≤ 30 ms mean
// over a 10,000-line C# document containing 50 marker-annotated literals
// with a total of ~500 interpolation holes (avg 10 holes/literal).
// Executed as part of the test suite (npm run test:perf) and hard-fails the
// run when the threshold is exceeded.

const LINES_TARGET = 10_000;
const MARKER_COUNT = 50;
const HOLES_PER_LITERAL = 10;
const ITERATIONS = 100;
const MAX_MEAN_MS = 30;

function buildInterpLiteralLine(index: number): string {
  // Build a marker-annotated interpolated literal with HOLES_PER_LITERAL holes.
  // Each hole holds a unique variable reference so the parser actually walks
  // HOLES_PER_LITERAL brace pairs on every call.
  const holes = Array.from(
    { length: HOLES_PER_LITERAL },
    (_, k) => `<span>{v${index}_${k}}</span>`,
  ).join("");
  return `        var m${index} = /*lang=html*/$"<p>${holes}</p>";`;
}

function buildSyntheticDocument(): string {
  const plainLine = '        var ignored = "plain literal no markers here";';

  const lines: string[] = [];
  lines.push("using System;");
  lines.push("public class Benchmark {");
  lines.push("    public void Method() {");

  const bodyTarget = LINES_TARGET - 5;
  const stride = Math.floor(bodyTarget / MARKER_COUNT);
  let markerIdx = 0;
  for (let i = 0; i < bodyTarget; i++) {
    if (i % stride === 0 && markerIdx < MARKER_COUNT) {
      lines.push(buildInterpLiteralLine(markerIdx));
      markerIdx++;
    } else {
      lines.push(plainLine);
    }
  }

  lines.push("    }");
  lines.push("}");
  return lines.join("\n");
}

function run(): void {
  const doc = buildSyntheticDocument();
  const regions = parseHtmlRegions(doc);
  if (regions.length !== MARKER_COUNT) {
    throw new Error(
      `synthetic document invariant violated: expected ${MARKER_COUNT} regions, got ${regions.length}`,
    );
  }
  const totalHoles = regions.reduce((sum, r) => sum + r.holes.length, 0);
  const expectedHoles = MARKER_COUNT * HOLES_PER_LITERAL;
  if (totalHoles !== expectedHoles) {
    throw new Error(
      `hole count invariant violated: expected ${expectedHoles} holes, got ${totalHoles}`,
    );
  }

  // Warm-up to prime V8 optimizations.
  for (let i = 0; i < 5; i++) parseHtmlRegions(doc);

  const timings: number[] = [];
  for (let i = 0; i < ITERATIONS; i++) {
    const start = process.hrtime.bigint();
    parseHtmlRegions(doc);
    const end = process.hrtime.bigint();
    timings.push(Number(end - start) / 1e6);
  }

  timings.sort((a, b) => a - b);
  const mean = timings.reduce((a, b) => a + b, 0) / timings.length;
  const p95 = timings[Math.floor(timings.length * 0.95)];
  const max = timings[timings.length - 1];

  const lineCount = doc.split("\n").length;
  console.log(
    `parseHtmlRegions — doc=${lineCount} lines, markers=${regions.length}, holes=${totalHoles}, iterations=${ITERATIONS}`,
  );
  console.log(
    `  mean=${mean.toFixed(2)}ms  p95=${p95.toFixed(2)}ms  max=${max.toFixed(2)}ms  (target mean ≤ ${MAX_MEAN_MS}ms)`,
  );

  if (mean > MAX_MEAN_MS) {
    console.error(`FAIL: mean ${mean.toFixed(2)}ms exceeds target ${MAX_MEAN_MS}ms`);
    process.exit(1);
  }
}

run();
